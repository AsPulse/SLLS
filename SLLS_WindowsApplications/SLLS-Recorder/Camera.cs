using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly int frameLength = 240;
        private int renderedFrame = 0;
        private int chunkId = 0;

        private VideoWriter? vw;

        Stopwatch sw = new();

        public Camera() {
            Task.Run(Worker_DoWork);
            startRecord();
        }

        private void Worker_DoWork() {
            VideoCapture vc = new(4) {
                FrameWidth = width,
                FrameHeight = height
            };

            Mat frame = new();
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
                    if (renderedFrame == 0 || renderedFrame >= frameLength) {
                        chunkId++;
                        Debug.WriteLine(renderedFrame);
                        renderedFrame = 0;
                        sw.Restart();
                        vw?.Dispose();
                        vw = new(string.Format("./data/{0}.mp4", chunkId), FourCC.H264, fps, new Size(width, height));

                    }
                    int nowFrame = (int) Math.Round(sw.Elapsed.TotalSeconds * fps);
                    while(renderedFrame <= nowFrame && renderedFrame < frameLength) {
                        vw?.Write(frame);
                        renderedFrame++;
                    }
                }
            }
        }

        public void Dispose() {
            live = false;
            stopRecord();
        }
        
        public void startRecord() {
            recording = true;
            renderedFrame = 0;
            chunkId = 0;
            sw.Restart();
        }

        public void stopRecord() {
            recording = false;
            vw?.Dispose();
            renderedFrame = 0;
            chunkId = 0;
        }
    }
}
