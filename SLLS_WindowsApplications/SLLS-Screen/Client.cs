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

namespace SLLS_Screen {
    internal class Client {

        public delegate void DisposeEventHandler(object sender);
        public event DisposeEventHandler? OnDispose;

        private bool Disposed = false;

        private TcpClient? client;

        private byte OwnDeviceId = 0xFF;

        public Action<string>? Logger;
        readonly ITimeProvider Time;


        public Client(string host, int port, Action<string> logger, Action<object> initialDisposer, ITimeProvider time) {
            Logger = logger;
            Time = time;

            if (initialDisposer != null) {
                OnDispose += o => initialDisposer.Invoke(o);
            }
            try {
                IPEndPoint localEndPoint = new(IPAddress.Parse("127.0.0.1"), 0);
                IPEndPoint remoteEndPoint = new(Dns.GetHostEntry(host).AddressList.First(v => v.AddressFamily == AddressFamily.InterNetwork), port);

                client = new TcpClient(localEndPoint);
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
        }

        private void Receive(ManagedPayload payload) {
            if(payload is AssignDeviceId assignDeviceId) {
                OwnDeviceId = assignDeviceId.TargetDeviceId;
                return;
            }
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
