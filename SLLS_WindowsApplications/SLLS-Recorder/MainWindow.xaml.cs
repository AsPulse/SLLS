using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;


namespace SLLS_Recorder {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Camera camera;

        private int frames = 0;
        private Stopwatch time;

        public MainWindow()
        {
            InitializeComponent();

            camera = new Camera();
            camera.CameraImageRefreshed += Camera_ImageRefreshed;

            time = new Stopwatch();
            time.Start();
        }

        private void Camera_ImageRefreshed(object sender, WriteableBitmap bitmap) {
            CameraViewer.Source = bitmap;

            frames++;
            if(frames % 3 == 0) {
                PlaybackInfo.Content = string.Format("PlaybackInfo: fps={0}", Math.Round(frames / time.Elapsed.TotalSeconds, 1));
                if (time.ElapsedMilliseconds > 1000) {
                    frames = 0;
                    time.Restart();
                }
            }
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            camera.Dispose();
        }
    }
}
