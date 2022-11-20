using SLLS_Common.ManagedPayloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common {
    public abstract class ManagedPayload {
        public TCPPayload? Raw { get; set; }

        protected ManagedPayload(TCPPayload? raw = null) {
            Raw = raw;
        }

        public byte DeviceId { get; set; }

        public const byte SERVER_DEVICEID = 0xF0;

        public abstract SendablePayload SendData(byte ToDeviceId);

        public abstract string ToLogStringReceive();

        public static string LogStringSend(byte ToDeviceId, string content) {
            return $"<-- 0x{ToDeviceId:X2} {content}";
        }

        protected string LogStringReceive(string content) {
            return $"--> 0x{DeviceId:X2} {content}";
        }

    }

    public class SendablePayload {
        public readonly byte[] Data;
        public readonly byte ToDeviceId;
        public string Log;

        public SendablePayload(byte[] data, byte toDeviceId, string log) {
            Data = data;
            ToDeviceId = toDeviceId;
            Log = ManagedPayload.LogStringSend(toDeviceId, log);
        }
    }
}

