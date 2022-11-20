using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SLLS_Common;
using SLLS_Common.ManagedPayloads.DeviceId;
using SLLS_Recorder.Recording;

namespace SLLS_Recorder.Streaming {
    internal class Server {
        private readonly Func<bool> IsActive;

        private readonly SLLSListener? Listener;
        private readonly SynchronizedCollection<TcpClient> Clients = new();
        private readonly SLLSClientController SLLSClients = new();
        private readonly ClockTimeProvider Time = new();

        private readonly Camera TargetCamera;

        public List<Chunk> Chunks = new();

        private bool Disposed = false;

        public Action<string>? Logger;

        public int Port { get; }

        public delegate void DisposeEventHandler(object sender);
        public event DisposeEventHandler? OnDispose;


        private void Receive(ManagedPayload payload) {
            if (payload is RequestDeviceId requestDeviceId) {
                if (requestDeviceId.Raw == null) return;
                Reply(
                    new AssignDeviceId() {
                        DeviceId = ManagedPayload.SERVER_DEVICEID,
                        TargetDeviceId = SLLSClients.NewDevice(requestDeviceId.Raw.Client, this)
                    },
                    payload
                );
                return;

            }
            if (payload is DownloadChunkVideo downloadChunkVideo) {
                DisposeChunk();
                long target = downloadChunkVideo.ChunkId;
                Chunk? c = Chunks.FirstOrDefault(v => v.id == target);
                if (c == null) {
                    Reply(
                        new SendChunkVideo() {
                            Available = false,
                            ChunkId = target
                        },
                        payload
                    );
                    return;
                }
                Reply(
                    new SendChunkVideo() {
                        Available = true,
                        ChunkId = target,
                        Length = c.size,
                        Data = c.data,
                    },
                    payload
                );
                return;
            }
        }

        public Server(int port, Action<string> logger, Action<object> initialDisposer, Camera camera) {
            Logger = logger;
            TargetCamera = camera;
            Port = port;
            IsActive = () => Listener != null && Listener.Active;
            if (initialDisposer != null) {
                OnDispose += o => initialDisposer.Invoke(o);
            }
            try {
                IPEndPoint localEndPoint = new(IPAddress.Any, port);
                Listener = new(localEndPoint);
                Listener.Start();
                TargetCamera.NewChunkReleased += NewChunk;
                AsyncAcceptWaitLoop();
            } catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse) {
                logger?.Invoke("Error: Address is already in use!");
                Dispose();
            } catch {
                logger?.Invoke("Error: Server cannot start because of Unknown Reason.");
                Dispose();
            }
        }

        private void DisposeChunk() {
            long now = Time.Now();
            Chunks.Where(v => now > v.id + v.length + 2000).ToList().ForEach(v => {
                Logger?.Invoke($"Chunk {v.id} expired.");
                Chunks.Remove(v);
            });
        }

        private void NewChunk(object sender, Chunk chunk) {
            DisposeChunk();
            Chunks.Add(chunk);
            if (!IsActive.Invoke()) return;
            Broadcast(new PushNewChunk() {
                DeviceId = ManagedPayload.SERVER_DEVICEID,
                ChunkId = chunk.id
            });
        }

        private Task AsyncAcceptWaitLoop() {
            Logger?.Invoke($"Server Listening on TCP/{Port}...");
            return Task.Run(() => {
                while (IsActive.Invoke()) {
                    try {
                        Listener?.BeginAcceptTcpClient(AcceptCallback, null).AsyncWaitHandle.WaitOne(-1);
                    } catch {
                        Logger?.Invoke("Error: cannot accept new connection.");
                    }
                }
            });
        }

        private void AcceptCallback(IAsyncResult result) {
            if (Listener == null || !IsActive.Invoke()) return;
            try {
                TcpClient tcpClient = Listener.EndAcceptTcpClient(result);
                Logger?.Invoke($"--> New Connection {tcpClient.Client.RemoteEndPoint}");
                Clients.Add(tcpClient);

                TCPPayload payload = new(tcpClient);
                tcpClient.Client.BeginReceive(payload.Data, 0, payload.Data.Length, SocketFlags.None, ReceiveCallback, payload);
            } catch { }
        }

        private void ReceiveCallback(IAsyncResult result) {
            try {
                if (result.AsyncState is TCPPayload payload) {
                    int length = payload.Client.Client.EndReceive(result);
                    if (length <= 0) {
                        SLLSClients.Release(payload.Client);
                        Logger?.Invoke($"--> Disconnected {payload.Client.Client.RemoteEndPoint}");
                        Clients.Remove(payload.Client);
                        return;
                    }

                    //Receive Process
                    payload.MarkReceived(Time);
                    ManagedPayload? parsed = payload.Parse();
                    if (parsed != null) {
                        Logger?.Invoke(parsed.ToLogStringReceive());
                        Receive(parsed);
                    }

                    if (IsActive.Invoke()) {
                        TCPPayload nextPayload = new(payload.Client);
                        payload.Client.Client.BeginReceive(nextPayload.Data, 0, nextPayload.Data.Length, SocketFlags.None, ReceiveCallback, nextPayload);
                    }
                }
            } catch { }
        }

        private void Send(byte ToDeviceId, ManagedPayload payload, TcpClient c) {
            SendablePayload sendablePayload = payload.SendData(ToDeviceId);
            c.Client.Send(sendablePayload.Data);
            Logger?.Invoke(sendablePayload.Log);

        }

        private void Reply(ManagedPayload payload, ManagedPayload received) {
            if (received.Raw == null) return;
            Send(received.DeviceId, payload, received.Raw.Client);
        }

        private void Broadcast(ManagedPayload payload) {
            SLLSClients.List.ToList().ForEach(c => {
                Send(c.DeviceId, payload, c.TcpClient);
            });
        }

        public Task Dispose() {
            if (Disposed) return Task.CompletedTask;
            Disposed = true;
            return Task.Run(() => {
                Listener?.Stop();
                lock (Clients.SyncRoot) {
                    foreach (TcpClient c in Clients) {
                        Logger?.Invoke($"<-- Socket Close {c.Client.RemoteEndPoint}");
                        SLLSClients.Release(c);
                        c.Client.Close();
                    }
                }
                OnDispose?.Invoke(this);
                Logger?.Invoke("Listener Stopped.");
            });
        }
    }
}
