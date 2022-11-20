namespace SLLS_Common.ManagedPayloads.DeviceId {
    public class DownloadChunkVideo : ManagedPayload
    {
        public const byte ENDPOINT = 0x13;

        public DownloadChunkVideo(TCPPayload? raw = null) : base(raw) { }

        public long ChunkId { get; set; }

        public static DownloadChunkVideo? Parse(TCPPayload payload)
        {
            if (payload.Data[0] != ENDPOINT) return null;
            return new(payload)
            {
                DeviceId = payload.Data[1],
                ChunkId = BitConverter.ToInt64(payload.Data.AsSpan()[3..11])
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
                $"DOWNLOAD_CHUNK_VIDEO: {ChunkId}"
            );
        }

        public override string ToLogStringReceive()
        {
            return LogStringReceive($"DOWNLOAD_CHUNK_VIDEO: {ChunkId}");
        }
    }
}
