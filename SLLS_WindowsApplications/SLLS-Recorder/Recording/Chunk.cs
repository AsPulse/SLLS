using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder.Recording {
    internal class Chunk {
        public long id;
        public byte[] data;
        public int length;

        public int frames;

        public Chunk(long id, byte[] data, int frames) {
            this.id = id;
            this.data = data;
            this.frames = frames;
            length = data.Length;
        }
    }
}
