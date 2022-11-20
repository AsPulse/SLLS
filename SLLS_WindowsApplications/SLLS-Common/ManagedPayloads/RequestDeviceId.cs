using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common.ManagedPayloads {
    public class RequestDeviceId : ManagedPayload {
        public const byte ENDPOINT = 0x00;

        public static RequestDeviceId? Parse(TCPPayload payload) {
            if (payload.Data[0] != ENDPOINT) return null;
            return new RequestDeviceId();
        }

        public byte[] ToByte() {
            return new byte[] { ENDPOINT, 0xFF };
        }
    }
}
