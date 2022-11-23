using System.Security.Cryptography;

namespace SLLS_Common.ManagedPayloads.DeviceId {
    public class SendChunkVideo : ManagedPayload
    {
        public const byte ENDPOINT = 0x14;

        public SendChunkVideo(TCPPayload? raw = null) : base(raw) { }

        public bool Available { get; set; }
        public long ChunkId { get; set; }
        public int Length { get; set; }
        public byte[]? Data { get; set; }
        public byte[]? Hashes { get; set; }

        public static SendChunkVideo? Parse(TCPPayload payload)
        {
            if (payload.Data[0] != ENDPOINT) return null;

            long chunkId = BitConverter.ToInt64(payload.Data.AsSpan()[3..11]);

            if (payload.Data[11] == 0x00) { 
                return new(payload)
                {
                    DeviceId = payload.Data[1],
                    Available = false,
                    ChunkId = chunkId,
                };

            }

            int payloadLen = BitConverter.ToInt32(payload.Data.AsSpan()[44..48]);
            return new(payload) {
                DeviceId = payload.Data[1],
                Available = true,
                ChunkId = chunkId,
                Hashes = payload.Data.AsSpan()[12..32].ToArray(),
                Length = payloadLen,
                Data = payload.Data.AsSpan()[48..(48 + payloadLen)].ToArray(),
            };
        }

        public override SendablePayload SendData(byte ToDeviceId)
        {
            BytePacket packet = new(); 
            packet.Append(new byte[] { ENDPOINT, DeviceId, ToDeviceId });
            packet.Append(ChunkId);
            if (Available && Data != null) {
                Hashes ??= Hash.GetHash(Data);
                packet.Append((byte)0x01);
                packet.Append(Hashes);
                packet.Append(Length);
                packet.Append(Data);

            } else {
                packet.Append((byte)0x00);
            }
            return new(
                packet.ToPacket(),
                ToDeviceId,
                $"SEND_CHUNK_VIDEO: {ChunkId}"
            );;
        }

        public override LogObject ToLogStringReceive()
        {
            return LogStringReceive($"SEND_CHUNK_VIDEO: {ChunkId} {(Available ? "(available)" : "(unavailable)")}");
        }
    }
}
