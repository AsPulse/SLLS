using SLLS_Common.ManagedPayloads;
using System.Net.Sockets;

namespace SLLS_Common {
    public class TCPPayload {
        public TcpClient Client { get; private set; }

        public TCPPayload(TcpClient client) {
            Client = client;
        }

        public byte[] Data = new byte[1024 * 1024 * 30];

        public ManagedPayload? Parse() {
            return Data[0] switch {
                AssignDeviceId.ENDPOINT => AssignDeviceId.Parse(this),
                RequestDeviceId.ENDPOINT => RequestDeviceId.Parse(this),
                _ => null,
            };
        }
    }
}
