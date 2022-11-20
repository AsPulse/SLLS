using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common.ManagedPayloads {
    public class AssignDeviceId : ManagedPayload {
        public const byte ENDPOINT = 0x01;
        public byte DeviceId { get; set; }

        public static AssignDeviceId? Parse(TCPPayload payload) {
            if (payload.Data[0] != ENDPOINT) return null;
            return new () {
                DeviceId = payload.Data[1],
            };
        }

        public byte[] ToByte() {
            return new byte[] { ENDPOINT, ManagedPayload.SERVER_DEVICEID, DeviceId };
        }
    }
}
