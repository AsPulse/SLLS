using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder.Recording {
    internal class Chunk {
        public long id;
        public byte[]? data = null;

        public int length;

        readonly Stopwatch sw = new();
        long? TimeToFetch = null;

        public Chunk(long id, int length) {
            this.id = id;
            this.length = length;
        }

        public void StartDownload() {
            sw.Reset();
            sw.Start();
        }

        public void Ready(byte[] bytes) {
            TimeToFetch = sw.ElapsedMilliseconds;
            Debug.WriteLine($"Id: {id}, Size: {bytes.Length}, Time: {TimeToFetch}");
            data = bytes;
        }
    }
}
