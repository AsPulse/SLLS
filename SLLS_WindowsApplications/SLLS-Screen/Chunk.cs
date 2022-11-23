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

        public bool ready = false;

        private VideoCapture? Vc = null;
        int? Seeking = null;
        public Mat? Mat = null;

        private bool disposed = false;
        readonly string filepath;

        public Chunk(long id, int length) {
            this.id = id;
            this.length = length;
            filepath = $"./data/chunk_{id}.mp4";
        }

        int seekingProcesses = 0;
        public void StartDownload() {
            sw.Reset();
            sw.Start();
        }

        public void Ready(byte[] bytes) {
            _ = Task.Run(async () => {
                string? dir = Path.GetDirectoryName(filepath);
                if (dir == null) throw new Exception("GetDirectoryName Failed!");

                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                using BinaryWriter writer = new(new FileStream(filepath, FileMode.CreateNew));
                writer.Write(bytes);
                writer.Close();
                await writer.DisposeAsync();


                Vc = new(filepath);

                Seeking = 0;
                Mat ??= new();
                Vc.Read(Mat);
                ready = true;
                TimeToFetch = sw.ElapsedMilliseconds;
                Debug.WriteLine($"Id: {id}, Size: {bytes.Length}, Time: {TimeToFetch}");

            });
        }

        public void SeekBitmap(int target, WriteableBitmap bitmap) {
            seekingProcesses++;
            if (disposed || Vc == null  || (Mat != null && !Mat.IsContinuous())) {
                seekingProcesses--;
                return;
            }
            if (Seeking != null && target <= Seeking) {
                if(Mat != null && Seeking == 0) Write(bitmap);
                seekingProcesses--;
                return;
            };
            Mat ??= new();
            while (target > Seeking) {
                Vc.Read(Mat);
                Seeking++;
            }
            Write(bitmap);
            seekingProcesses--;
        }

        private void Write(WriteableBitmap bitmap) {
            if (disposed) return;
            if (Mat == null) return;
            Application.Current.Dispatcher.Invoke(() => {
                if (Mat.Width != bitmap.Width || Mat.Height != bitmap.Height) {
                    Debug.WriteLine("Wrong Mat Size!");
                    Mat.Dispose();
                    Mat = null;
                    ready = false;
                    File.Copy(filepath, "./data/error!.mp4");
                    Application.Current.Shutdown();
                    return;
                };
                WriteableBitmapConverter.ToWriteableBitmap(Mat, bitmap);
            });

        }
        public void Dispose() {
            Task.Run(async () => {
                while ( seekingProcesses > 0 ) {
                    await Task.Delay(1000);
                }
                if (disposed) return;
                disposed = true;
                Mat?.Dispose();
                Mat = null;
                Vc?.Dispose();
                Vc = null;
                if (File.Exists(filepath)) File.Delete(filepath);
                GC.Collect();
            });
        }

    }
}
