namespace SLLS_Common.ManagedPayloads.DeviceId {
    public class PushNewChunk : ManagedPayload
    {
        public const byte ENDPOINT = 0x12;

        public PushNewChunk(TCPPayload? raw = null) : base(raw) { }

        public long ChunkId;

        public static PushNewChunk? Parse(TCPPayload payload)
        {
            if (payload.Data[0] != ENDPOINT) return null;
            return new(payload)
            {
                DeviceId = payload.Data[1],
            };
        }

        public override SendablePayload SendData(byte ToDeviceId)
        {
            BytePacket packet = new();
            packet.Append(new byte[] { ENDPOINT, DeviceId, ToDeviceId });
            packet.Append(ChunkId);

            return new(
                packet.ToPacket(),
                ToDeviceId,
                $"New Packet: {ChunkId}"
            );
        }

        public override string ToLogStringReceive()
        {
            return LogStringReceive($"New Packet: {ChunkId}");
        }
    }
}
