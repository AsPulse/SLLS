using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common.ManagedPayloads {
    public class AssignDeviceId : ManagedPayload {
        public const byte ENDPOINT = 0x01;

        public AssignDeviceId(TCPPayload? raw = null) : base(raw) { }


        public byte TargetDeviceId { get; set; }

        public static AssignDeviceId? Parse(TCPPayload payload) {
            if (payload.Data[0] != ENDPOINT) return null;
            return new (payload) {
                DeviceId = payload.Data[1],
                TargetDeviceId = payload.Data[2],
            };
        }

        public override byte[] ToByte() {
            return new byte[] { ENDPOINT, DeviceId, TargetDeviceId };
        }

        public override string ToLogStringSend() {
            return $"<-- ASSIGN_DEVICEID Assigned: 0x{TargetDeviceId:X2}";
        }
        public override string ToLogStringReceive() {
            return $"--> 0x{DeviceId:X2} ASSIGN_DEVICEID";
        }
    }
}
