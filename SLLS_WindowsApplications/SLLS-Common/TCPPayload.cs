using System.Net.Sockets;

namespace SLLS_Common {
    public class TCPPayload {
        public TcpClient Client { get; private set; }

        public TCPPayload(TcpClient client) {
            Client = client;
        }

        public byte[] Data = new byte[1024 * 1024 * 30];
    }
}
