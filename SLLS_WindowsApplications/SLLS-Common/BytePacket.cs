using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common {
    internal class BytePacket {
        private long length = 0;
        private readonly List<ByteSegment> Contents = new();
        private bool Exported = false;

        public void Append(byte[] d) {
            Contents.Add(new ByteSegment(length, d.Length, d));
            length += d.Length;
        }
        public void Append(byte d) {
            Append(new byte[] { d });
        }

        public void Append(int d) {
            Append(BitConverter.GetBytes(d));
        }

        public void Append(long d) {
            Append(BitConverter.GetBytes(d));
        }

        public byte[] ToPacket() {
            if (Exported) throw new Exception();
            Exported = true;
            byte[] bytes = new byte[length];
            Contents.ForEach(s => {
                Array.Copy(s.Bytes, 0, bytes, s.StartPoint, s.Length);
            });
            Contents.Clear();
            return bytes;
        }

        class ByteSegment {
            public long StartPoint;
            public long Length;
            public byte[] Bytes;

            public ByteSegment(long startPoint, long length, byte[] bytes) {
                StartPoint = startPoint;
                Length = length;
                Bytes = bytes;
            }
        }
    }
}
