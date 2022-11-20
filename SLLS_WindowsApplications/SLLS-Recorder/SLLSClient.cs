using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder {
    internal class SLLSClient {
        public TcpClient TcpClient;
        public byte DeviceId;
        public string FriendlyName;

        public SLLSClient(TcpClient tcpClient, byte deviceId) {
            TcpClient = tcpClient;
            DeviceId = deviceId;
            FriendlyName = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }
    }
}
