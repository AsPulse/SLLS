using System.Net.Sockets;
using SLLS_Common.ManagedPayloads.DeviceId;

namespace SLLS_Common {
    public class TCPPayload {
        public TcpClient Client { get; private set; }

        public TCPPayload(TcpClient client) {
            Client = client;
        }

        public byte[] Data = new byte[1024 * 1024 * 30];

        public long Received = -1;

        public void MarkReceived(ITimeProvider time) {
            Received = time.Now();
        }

        public ManagedPayload? Parse() {
            return Data[0] switch {
                AssignDeviceId.ENDPOINT => AssignDeviceId.Parse(this),
                RequestDeviceId.ENDPOINT => RequestDeviceId.Parse(this),
                PushNewChunk.ENDPOINT => PushNewChunk.Parse(this),
                _ => null,
            };
        }
    }
}
