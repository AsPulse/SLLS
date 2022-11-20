using System.Net.Sockets;

namespace SLLS_Recorder.Streaming {
    internal class SLLSClient
    {
        private readonly Server Server;
        public TcpClient TcpClient;
        public byte DeviceId;
        public string FriendlyName;

        public SLLSClient(Server server, TcpClient tcpClient, byte deviceId)
        {
            Server = server;
            TcpClient = tcpClient;
            DeviceId = deviceId;
            FriendlyName = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        internal void Disconnect()
        {
            Server.logger?.Invoke($"<-- Device lost connection: 0x{DeviceId:X2}");
        }
    }
}
