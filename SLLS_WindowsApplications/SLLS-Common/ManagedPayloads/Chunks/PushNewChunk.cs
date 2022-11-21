namespace SLLS_Common.ManagedPayloads.DeviceId {
    public class PushNewChunk : ManagedPayload
    {
        public const byte ENDPOINT = 0x12;

        public PushNewChunk(TCPPayload? raw = null) : base(raw) { }

        public long ChunkId { get; set; }
        public int ChunkLength { get; set; }

        public static PushNewChunk? Parse(TCPPayload payload)
        {
            if (payload.Data[0] != ENDPOINT) return null;
            return new(payload)
            {
                DeviceId = payload.Data[1],
                ChunkId = BitConverter.ToInt64(payload.Data.AsSpan()[3..11]),
                ChunkLength = BitConverter.ToInt32(payload.Data.AsSpan()[11..15])
            };
        }

        public override SendablePayload SendData(byte ToDeviceId)
        {
            BytePacket packet = new();
            packet.Append(new byte[] { ENDPOINT, DeviceId, ToDeviceId });
            packet.Append(ChunkId);
            packet.Append(ChunkLength);

            return new(
                packet.ToPacket(),
                ToDeviceId,
                $"PUSH_NEW_CHUNK: {ChunkId} (Length {ChunkLength})"
            );
        }

        public override string ToLogStringReceive()
        {
            return LogStringReceive($"PUSH_NEW_CHUNK: {ChunkId} (Length {ChunkLength})");
        }
    }
}
