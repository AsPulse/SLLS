using System;   
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Window = OpenCvSharp.Window;

namespace SLLS_Recorder {
    internal class Camera : IDisposable {

        public delegate void CameraImageRefreshedEventHandler(object sender, WriteableBitmap bitmap);
        public event CameraImageRefreshedEventHandler? CameraImageRefreshed;

        private bool live = true;

        public Camera() {
            Task.Run(Worker_DoWork);
        }

        private void Worker_DoWork() {
            VideoCapture vc = new(4) {
                FrameWidth = 1920,
                FrameHeight = 1080
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
            }
        }

        public void Dispose() {
            live = false;
        }

    }
}
