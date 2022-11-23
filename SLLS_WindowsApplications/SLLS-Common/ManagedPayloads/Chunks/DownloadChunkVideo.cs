namespace SLLS_Common.ManagedPayloads.DeviceId {
    public class DownloadChunkVideo : ManagedPayload
    {
        public const byte ENDPOINT = 0x13;

        public DownloadChunkVideo(TCPPayload? raw = null) : base(raw) { }

        public long ChunkId { get; set; }

        public bool Errored { get; set; }

        public static DownloadChunkVideo? Parse(TCPPayload payload)
        {
            if (payload.Data[0] != ENDPOINT) return null;
            return new(payload)
            {
                DeviceId = payload.Data[1],
                ChunkId = BitConverter.ToInt64(payload.Data.AsSpan()[3..11]),
                Errored = payload.Data[11] == 0x01
            };
        }

        public override SendablePayload SendData(byte ToDeviceId)
        {
            BytePacket packet = new();
            packet.Append(new byte[] { ENDPOINT, DeviceId, ToDeviceId });
            packet.Append(ChunkId);
            packet.Append((byte)(Errored ? 0x01 : 0x00));

            return new(
                packet.ToPacket(),
                ToDeviceId,
                $"{(Errored ? "HASH_CONFLICTED # " : "")}DOWNLOAD_CHUNK_VIDEO: {ChunkId}",
                Errored ? LOG_SEVERITY.WARN : LOG_SEVERITY.INFO
            );
        }

        public override LogObject ToLogStringReceive()
        {
            return LogStringReceive($"{(Errored ? "HASH_CONFLICTED # " : "")}DOWNLOAD_CHUNK_VIDEO: {ChunkId}", Errored ? LOG_SEVERITY.WARN : LOG_SEVERITY.INFO);
        }
    }
}
