using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Size = OpenCvSharp.Size;

namespace SLLS_Recorder {
    internal class Camera : IDisposable {

        public delegate void CameraImageRefreshedEventHandler(object sender, WriteableBitmap bitmap);
        public event CameraImageRefreshedEventHandler? CameraImageRefreshed;

        private bool live = true;

        private readonly int width = 1920;
        private readonly int height = 1080;
        private readonly int fps = 24;

        private bool recording = false;
        private readonly int chunkLength = 240;
        private int renderedFrame = 0;
        private int chunkId = 0;

        private int dropFrames = 0;

        private FFMpegWriterController vw;
        readonly Stopwatch sw = new();

        public Camera() {
            vw = new(chunkId =>
            new FFMpegWriter(fps, width, height, string.Format("./data/{0}.mp4", chunkId))
        );
            Task.Run(Worker_DoWork);
            Task.Run(async () => {
                await Task.Delay(3000);
                StartRecord();
            });
        }

        private void Worker_DoWork() {
            VideoCapture vc = new(4) {
                FrameWidth = width,
                FrameHeight = height
            };

            Mat frame = new(width, height, MatType.CV_8UC3);
            if (!vc.IsOpened()) {
                MessageBox.Show("Can't use camera.");
                return;
            }

            while (live) {
                vc.Read(frame);
                if (frame.Empty()) {
                    MessageBox.Show("Mat is empty.");
                    break;
                }
                WriteableBitmap bmp = frame.ToWriteableBitmap();
                bmp.Freeze();
                Application.Current.Dispatcher.Invoke(() => {
                    CameraImageRefreshed?.Invoke(this, bmp);
                });

                /**
                 * Recording
                 */
                if (recording) {
                    int nowFrame = (int) Math.Round(sw.Elapsed.TotalSeconds * fps);
                    int skipped = nowFrame - renderedFrame - 1;
                    if (skipped > 0) {
                        dropFrames += skipped;
                        Debug.WriteLine(string.Format("Dropped: {0} ( of {1} )",skipped, dropFrames));
                    }
                    while(renderedFrame < nowFrame && renderedFrame < chunkLength) {
                        vw.GetVw(chunkId).Result.SetFrame(renderedFrame, frame);
                        renderedFrame++;
                    }
                    if (renderedFrame >= chunkLength) {
                        vw.RenderChunk(chunkId++);
                        Task.Run(() => {
                            Enumerable.Range(chunkId, 5).ToList().ForEach(x => vw.Ready(x));
                        });
                        renderedFrame = 0;
                        sw.Restart();
                    }
                }
            }
        }

        public void Dispose() {
            if (!live) return;
            live = false;
            StopRecord().Wait();
        }
        
        public void StartRecord() {
            chunkId = 0;
            renderedFrame = 0;
            dropFrames = 0;
            Task.Run(async () => {
                await Task.WhenAll(Enumerable.Range(chunkId, 5).Select(x => vw.GetVw(x)));
                sw.Restart();
                recording = true;
                Debug.WriteLine("Recording Start");
            });
        }

        public Task StopRecord() {
            recording = false;
            renderedFrame = 0;
            int finishChunk = chunkId;
            chunkId = 0;
            return Task.Run(async () => {
                await vw.RenderChunk(finishChunk);
                await vw.FreeAllChunk();
            });
        }
    }
}
