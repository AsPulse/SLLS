using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder {
    internal class SLLSClient {
        private readonly Server Server;
        public TcpClient TcpClient;
        public byte DeviceId;
        public string FriendlyName;

        public SLLSClient(Server server, TcpClient tcpClient, byte deviceId) {
            Server = server;
            TcpClient = tcpClient;
            DeviceId = deviceId;
            FriendlyName = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        internal void Disconnect() {
            Server.logger?.Invoke($"<-- Device lost connection: 0x{DeviceId:X2}");
        }
    }
}
