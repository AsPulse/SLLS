using SLLS_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder {
    internal class Server {
        private readonly SLLSListener? Listener;
        private readonly SynchronizedCollection<TcpClient> Clients = new();

        private bool Disposed = false;

        public Action<string>? logger;

        public delegate void DisposeEventHandler(object sender);
        public event DisposeEventHandler? OnDispose;


        public Server(int port, Action<string> logger, Action<object> initialDisposer) {
            this.logger = logger;
            if(initialDisposer != null) { 
                OnDispose += o => initialDisposer.Invoke(o);
            }
            try { 
                IPEndPoint localEndPoint = new(IPAddress.Any, port);
                Listener = new(localEndPoint);
                Listener.Start();
                AsyncAcceptWaitLoop();
            } catch(SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse) {
                logger?.Invoke("Error: Address is already in use!");
                Dispose();
            } catch {
                logger?.Invoke("Error: Server cannot start because of Unknown Reason.");
                Dispose();
            }
        }


        private Task AsyncAcceptWaitLoop() {
            logger?.Invoke("Server Listening...");
            return Task.Run(() => {
                while (Listener != null && Listener.Active) {
                    try {
                        Listener.BeginAcceptTcpClient(AcceptCallback, null).AsyncWaitHandle.WaitOne(-1);
                    } catch {
                        logger?.Invoke("Error: cannot accept new connection.");
                    }
                }
            });
        }

        private void AcceptCallback(IAsyncResult result) {
            if (Listener == null) return;
            try {
                TcpClient tcpClient = Listener.EndAcceptTcpClient(result);
                logger?.Invoke($"--> New Connection {tcpClient.Client.RemoteEndPoint}");
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
                        logger?.Invoke($"--> Disconnected {payload.Client.Client.RemoteEndPoint}");
                        Clients.Remove(payload.Client);
                        return;
                    }
                    
                    //Receive Process

                    if (Listener != null && Listener.Active) {
                        TCPPayload nextPayload = new(payload.Client);
                        payload.Client.Client.BeginReceive(nextPayload.Data, 0, nextPayload.Data.Length, SocketFlags.None, ReceiveCallback, nextPayload);
                    }
                }
            } catch { }
        }

        public void Dispose() {
            if (Disposed) return;
            Disposed = true;
            Task.Run(() => {
                Listener?.Stop();
                lock (Clients.SyncRoot) {
                    foreach (TcpClient c in Clients) {
                        logger?.Invoke($"<-- Socket Close {c.Client.RemoteEndPoint}");
                        c.Client.Close();
                    }
                    Clients.Clear();
                }
                OnDispose?.Invoke(this);
                logger?.Invoke("Listener Stopped.");
            });
        }
    }
}