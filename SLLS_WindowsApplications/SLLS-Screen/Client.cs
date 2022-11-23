using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SLLS_Recorder;
using System.Windows.Media.Media3D;
using SLLS_Common;
using System.Net.Http;
using SLLS_Common.ManagedPayloads.DeviceId;
using SLLS_Recorder.Recording;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;

namespace SLLS_Screen {
    internal class Client {


        public readonly int width = 1920;
        public readonly int height = 1080;
        public readonly int fps = 24;

        public delegate void DisposeEventHandler(object sender);
        public event DisposeEventHandler? OnDispose;

        private bool Disposed = false;

        private TcpClient? client;

        public List<Chunk> Chunks = new();
        private byte OwnDeviceId = 0xFF;

        public Action<string>? Logger;
        readonly ITimeProvider Time;

        public readonly Projector Projector;
        private readonly WriteableBitmap bmp;

        public Client(string host, int port, Action<string> logger, Action<object> initialDisposer, ITimeProvider time, Projector projector) {
            Logger = logger;
            Time = time;
            Projector = projector;

            if (initialDisposer != null) {
                OnDispose += o => initialDisposer.Invoke(o);
            }

            bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
            Projector.Screen.Source = bmp;
            _ = ProjectorLoop();

            Task.Run(() => {
                try {
                    IPEndPoint localEndPoint = new(IPAddress.Any, 0);
                    IPEndPoint remoteEndPoint = new(Dns.GetHostEntry(host).AddressList.First(v => v.AddressFamily == AddressFamily.InterNetwork), port);

                    client = new TcpClient(localEndPoint) {
                        NoDelay = true
                    };
                    client.Connect(remoteEndPoint);

                    Logger?.Invoke($"<-- Connected to Server");

                    TCPPayload payload = new(client);
                    client.Client.BeginReceive(payload.Data, 0, payload.Data.Length, SocketFlags.None, ReceiveCallback, payload);

                    SendToServer(
                        new RequestDeviceId() {
                            DeviceId = OwnDeviceId,
                        }
                    );

                } catch {
                    logger?.Invoke("Error: Client cannot connect to server because of Unknown Reason.");
                    Dispose();
                }
            });
        }

        private Task ProjectorLoop() {
            return Task.Run(async () => {
                while (!Disposed) {
                    long time = Time.Now();
                    Chunk? chunk = Chunks.OrderByDescending(x => x.id).FirstOrDefault(x => x.id <= time && time <= x.id + x.length && x.ready);
                    if (chunk == null) continue;
                    int frame = (int)Math.Floor((time - chunk.id) / 1000.0 * fps);
                    await Task.Run(async () => {
                        chunk.SeekBitmap(frame, bmp);
                        await Task.Delay((int)(1000.0 / fps));
                    });
                }
            });
        }

        private void RequestChunk(Chunk c, bool errored) {
            SendToServer(
                new DownloadChunkVideo() {
                    DeviceId = OwnDeviceId,
                    ChunkId = c.id,
                    Errored = errored,
                }
            );
            c.StartDownload();
        }

        private void Receive(ManagedPayload payload) {
            DisposeChunk();
            if (payload is AssignDeviceId assignDeviceId) {
                OwnDeviceId = assignDeviceId.TargetDeviceId;
                return;
            }
            if(payload is PushNewChunk pushNewChunk) {
                Chunk c = new(pushNewChunk.ChunkId, pushNewChunk.ChunkLength);
                Chunks.Add(c);
                RequestChunk(c, false);
                return;
            }
            if(payload is SendChunkVideo sendChunkVideo) {
                Chunk? c = Chunks.FirstOrDefault(c => c.id == sendChunkVideo.ChunkId);
                if (!sendChunkVideo.Available || c == null || sendChunkVideo.Data == null || sendChunkVideo.Hashes == null) {
                    Chunks.RemoveAll(c => c.id == sendChunkVideo.ChunkId);
                    return;
                }
                if(!Hash.GetHash(sendChunkVideo.Data).SequenceEqual(sendChunkVideo.Hashes)) {
                    RequestChunk(c, true);
                    return;
                };
                c.Ready(sendChunkVideo.Data);
                return;
            }
        }

        private void DisposeChunk() {
            Task.Run(() => {
                long now = Time.Now();
                Chunks.Where(v => now > v.id + v.length + 5000).ToList().ForEach(v => {
                    v.Dispose();
                    Chunks.Remove(v);
                });
            });
        }


        private void ReceiveCallback(IAsyncResult result) {
            if (client == null) return;
            try {
                if (result.AsyncState is TCPPayload payload) {
                    int length = payload.Client.Client.EndReceive(result);
                    if (length <= 0) {
                        Logger?.Invoke($"--> Disconnected from Server");
                        return;
                    }

                    //Receive Process
                    payload.MarkReceived(Time);
                    ManagedPayload? parsed = payload.Parse();
                    if (parsed != null) {
                        Logger?.Invoke(parsed.ToLogStringReceive());
                        Receive(parsed);
                    }

                    TCPPayload nextPayload = new(payload.Client);
                    payload.Client.Client.BeginReceive(nextPayload.Data, 0, nextPayload.Data.Length, SocketFlags.None, ReceiveCallback, nextPayload);

                }
            } catch { }
        }

        private void Send(byte ToDeviceId, ManagedPayload payload, TcpClient c) {
            SendablePayload sendablePayload = payload.SendData(ToDeviceId);
            c.Client.Send(sendablePayload.Data);
            Logger?.Invoke(sendablePayload.Log);

        }


        private void SendToServer(ManagedPayload payload) {
            if (client == null) return;
            Send(ManagedPayload.SERVER_DEVICEID, payload, client);
        }

        public Task Dispose() {
            if (Disposed) return Task.CompletedTask;
            Disposed = true;
            client?.Close();
            client?.Dispose();
            client = null;
            return Task.Run(() => {                
                OnDispose?.Invoke(this);
                Logger?.Invoke("Client disconnected.");
            });
        }

    }
}
