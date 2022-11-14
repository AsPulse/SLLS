using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static OpenCvSharp.Stitcher;


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
            camera.StatusChanged += Camera_StatusChanged;
            CameraViewer.Source = camera.bmp;

            List<string> cameras = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
                .Select(v => v.Name)
                .ToList();
            cameras.ForEach(v => CameraSelection.Items.Add(v));
            int obsCameraIndex = cameras.FindIndex(v => v.Contains("OBS-Camera")); ;

            CameraSelection.SelectedIndex = obsCameraIndex >= 0 ? obsCameraIndex : 0;
        }

        private void Camera_StatusChanged(object sender, RecordingStatus status) {
            RecordingButton.IsEnabled = status == RecordingStatus.READY || status == RecordingStatus.RECORDING;
            switch (status) {
                case RecordingStatus.READY:
                    RecordingButton.Content = "録画";
                    break;
                case RecordingStatus.RECORDING:
                    RecordingButton.Content = "停止 (録画中...)";
                    break;
                case RecordingStatus.PREPARING_TO_START:
                    RecordingButton.Content = "録画開始処理中";
                    break;
                case RecordingStatus.PREPARING_TO_FINISH:
                    RecordingButton.Content = "録画停止処理中";
                    break;
            }
        }

        private void Camera_ImageRefreshed(object sender) {
            FrameCounter.Content = string.Format("Frames: {0} / {1} frames", camera.renderedFrame, camera.chunkLength);
            DroppedCounter.Content = string.Format("Dropped: {0} frames", camera.dropFrames);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            camera.Dispose();
        }

        private void CameraSelection_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            camera.SelectCamera(CameraSelection.SelectedIndex);
        }

        private void RecodingButton_Click(object sender, RoutedEventArgs e) {
            switch (camera.status) {
                case RecordingStatus.READY:
                    camera.StartRecord();
                    break;
                case RecordingStatus.RECORDING:
                    camera.StopRecord();
                    break;
            }
        }
    }
}
