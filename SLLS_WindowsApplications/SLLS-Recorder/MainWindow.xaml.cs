using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            List<string> cameras = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
                .Select(v => v.Name)
                .ToList();
            cameras.ForEach(v => CameraSelection.Items.Add(v));
            int obsCameraIndex = cameras.FindIndex(v => v.Contains("OBS-Camera")); ;

            CameraSelection.SelectedIndex = obsCameraIndex >= 0 ? obsCameraIndex : 0;
        }

        private void Camera_ImageRefreshed(object sender, WriteableBitmap bitmap) {
            CameraViewer.Source = bitmap;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            camera.Dispose();
        }

        private void CameraSelection_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            camera.SelectCamera(CameraSelection.SelectedIndex);
        }
    }
}
