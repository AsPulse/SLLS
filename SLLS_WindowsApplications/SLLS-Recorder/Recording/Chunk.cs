using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLLS_Common;

namespace SLLS_Recorder.Recording {
    internal class Chunk {
        public long id;
        public byte[] data;
        public byte[] hash;
        public int size;

        public int length;

        public Chunk(long id, byte[] data, int length) {
            this.id = id;
            this.data = data;
            this.length = length;
            size = data.Length;
            hash = Hash.GetHash(data);
        }
    }
}
