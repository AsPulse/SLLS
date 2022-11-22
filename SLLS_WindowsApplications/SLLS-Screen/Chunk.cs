using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace SLLS_Recorder.Recording {
    internal class Chunk {

        public long id;
        public int length;

        readonly Stopwatch sw = new();
        long? TimeToFetch = null;

        private VideoCapture? Vc = null;
        int? Seeking = null;
        public Mat? Mat = null;

        private bool disposed = false;

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
            _ = Task.Run(() => {
                string path = $"./data/chunk_{id}.mp4";
                string? dir = Path.GetDirectoryName(path);
                if (dir == null) throw new Exception("GetDirectoryName Failed!");

                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                using BinaryWriter writer = new(new FileStream(path, FileMode.CreateNew));
                writer.Write(bytes);
                writer.Close();

                Vc = new(path);

                Seeking = 0;
                Mat ??= new();
                Vc.Read(Mat);
            });
        }

        public void SeekBitmap(int target, WriteableBitmap bitmap) {
            if (disposed) return;
            if (Vc == null) return;
            if (Mat != null && !Mat.IsContinuous()) return;
            if (Seeking != null && target <= Seeking) {
                if(Mat != null && Seeking == 0) Write(bitmap);
                return;
            };
            if (Mat == null) return;
            while (target > Seeking) {
                Vc.Read(Mat);
                Seeking++;
            }
            Write(bitmap);
        }

        private void Write(WriteableBitmap bitmap) {
            if (disposed) return;
            if (Mat == null) return;
            Application.Current.Dispatcher.Invoke(() => {
                if (Mat.Width != bitmap.Width || Mat.Height != bitmap.Height) return;
                WriteableBitmapConverter.ToWriteableBitmap(Mat, bitmap);
            });

        }
        public void Dispose() {
            if (disposed) return;
            disposed = true;
            Mat?.Dispose();
            Mat = null;
            Vc?.Dispose();
            Vc = null;
            GC.Collect();
        }

    }
}
