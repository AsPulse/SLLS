namespace SLLS_Common {
    public abstract class ManagedPayload {
        public TCPPayload? Raw { get; set; }

        protected ManagedPayload(TCPPayload? raw = null) {
            Raw = raw;
        }

        public byte DeviceId { get; set; }

        public const byte SERVER_DEVICEID = 0xF0;

        public abstract SendablePayload SendData(byte ToDeviceId);

        public abstract LogObject ToLogStringReceive();

        public static string LogStringSend(byte ToDeviceId, string content) {
            return $"<-- 0x{ToDeviceId:X2} {content}";
        }

        protected LogObject LogStringReceive(string content, LOG_SEVERITY severity = LOG_SEVERITY.INFO) {
            return new LogObject { Content = $"--> 0x{DeviceId:X2} {content}", Severity = severity };
        }

    }

    public class SendablePayload {
        public readonly byte[] Data;
        public readonly byte ToDeviceId;
        public LogObject Log;

        public SendablePayload(byte[] data, byte toDeviceId, string log, LOG_SEVERITY severity = LOG_SEVERITY.INFO) {
            Data = data;
            ToDeviceId = toDeviceId;
            Log = new LogObject { Content = ManagedPayload.LogStringSend(toDeviceId, log), Severity = severity };
        }
    }
}

