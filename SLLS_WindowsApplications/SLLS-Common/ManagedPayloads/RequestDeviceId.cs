using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common.ManagedPayloads {
    public class RequestDeviceId : ManagedPayload {
        public const byte ENDPOINT = 0x00;

        public RequestDeviceId(TCPPayload? raw = null) : base(raw) { }

        public static RequestDeviceId? Parse(TCPPayload payload) {
            if (payload.Data[0] != ENDPOINT) return null;
            return new RequestDeviceId(payload) {
                DeviceId = payload.Data[1],
            };
        }

        public override byte[] ToByte() {
            return new byte[] { ENDPOINT, 0xFF };
        }

        public override string ToLogStringSend() {
            return "<-- REQUEST_DEVICEID";
        }
        public override string ToLogStringReceive() {
            return $"--> 0x{DeviceId:X2} REQUEST_DEVICEID";
        }
    }
}
