namespace SLLS_Common.ManagedPayloads.DeviceId {
    public class RequestDeviceId : ManagedPayload
    {
        public const byte ENDPOINT = 0x00;

        public RequestDeviceId(TCPPayload? raw = null) : base(raw) { }

        public static RequestDeviceId? Parse(TCPPayload payload)
        {
            if (payload.Data[0] != ENDPOINT) return null;
            return new RequestDeviceId(payload)
            {
                DeviceId = payload.Data[1],
            };
        }

        public override SendablePayload SendData(byte ToDeviceId)
        {
            return new(
                new byte[] { ENDPOINT, 0xFF, ToDeviceId },
                ToDeviceId,
                "REQUEST_DEVICEID"
            );
        }

        public override string ToLogStringReceive()
        {
            return $"--> 0x{DeviceId:X2} REQUEST_DEVICEID";
        }
    }
}
