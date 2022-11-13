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
        public MainWindow()
        {
            InitializeComponent();

            camera = new Camera();
            camera.CameraImageRefreshed += Camera_ImageRefreshed;

        }

        private void Camera_ImageRefreshed(object sender, WriteableBitmap bitmap) {
            CameraViewer.Source = bitmap;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            camera.Dispose();
        }
    }
}
