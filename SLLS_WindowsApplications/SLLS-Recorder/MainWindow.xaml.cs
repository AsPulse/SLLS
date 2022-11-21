using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DirectShowLib;
using SLLS_Recorder.Recording;
using SLLS_Recorder.Streaming;


namespace SLLS_Recorder {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly ClockTimeProvider Time = new();
        readonly Camera camera;
        Server? Server = null;
        

        private bool isCalledQuit = false;
        private bool isCleanuped = false;

        public MainWindow()
        {
            InitializeComponent();

            camera = new Camera();
            camera.CameraImageRefreshed += Camera_ImageRefreshed;
            camera.StatusChanged += Camera_StatusChanged;
            Camera_StatusChanged(camera, camera.status);

            CameraViewer.Source = camera.bmp;

            List<string> cameras = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
                .Select(v => v.Name)
                .ToList();
            cameras.ForEach(v => CameraSelection.Items.Add(v));
            int obsCameraIndex = cameras.FindIndex(v => v.Contains("OBS-Camera")); ;

            CameraSelection.SelectedIndex = obsCameraIndex >= 0 ? obsCameraIndex : 0;

            DispatcherTimer clock = new() {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            clock.Tick += (_, _) => {
                int msToSec = 1000;
                long now = Time.Now() + msToSec * 60 * 60 * 9;
                long hour = now % (msToSec * 60 * 60 * 24) / (msToSec * 60 * 60);
                long minute = now % (msToSec * 60 * 60) / (msToSec * 60);
                long second = now % (msToSec * 60) / msToSec;
                ServerClock.Content = $"SERVER CLOCK {hour:00}:{minute:00}:{second:00}";
            };
            clock.Start();
        }

        private void Camera_StatusChanged(object sender, RecordingStatus status) {
            RecordingButton.IsEnabled = status == RecordingStatus.READY || status == RecordingStatus.RECORDING;
            switch (status) {
                case RecordingStatus.READY:
                    RecordingButton.Content = "Start Transmission";
                    break;
                case RecordingStatus.RECORDING:
                    RecordingButton.Content = "Transmitting (click to stop)";
                    break;
                case RecordingStatus.PREPARING_TO_START:
                    RecordingButton.Content = "Start processing...";
                    break;
                case RecordingStatus.PREPARING_TO_FINISH:
                    RecordingButton.Content = "Stop processing...";
                    break;
            }
        }

        private void Camera_ImageRefreshed(object sender) {
            FrameCounter.Content = string.Format("Frames: {0} / {1} frames", camera.renderedFrame, camera.chunkLength);
            DroppedCounter.Content = string.Format("Dropped: {0} frames", camera.dropFrames);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (isCleanuped) return;
            e.Cancel = true;
            if (!isCalledQuit) Quit();
        }
        
        private void Quit() {
            Task.Run(async () => {
                isCalledQuit = true;
                await camera.Dispose();
                await (Server?.Dispose() ?? Task.CompletedTask);
                isCleanuped = true;
                Dispatcher.Invoke(() => Close());
            });
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

        public void ListboxLog(string content) {
            Logger.Items.Insert(0, content);
        }

        private void ListenControl_Click(object sender, RoutedEventArgs e) {
            if(Server == null) {
                int port = -1;
                if(int.TryParse(PortNumber.Text, out port)) {
                    Listen.Content = "Stop";
                    PortNumber.IsEnabled = false;
                    //Server Start
                    Server = new Server(
                        port,
                        s => {
                            Dispatcher.Invoke(() => ListboxLog(s));
                        },
                        _ => {
                            Dispatcher.Invoke(() => {
                                Listen.Content = "Listen";
                                PortNumber.IsEnabled = true;
                                Server = null;
                            });
                        },
                        camera,
                        Time
                    );
                } else {
                    ListboxLog("Unable Port Number");
                }
            } else {
                Server.Dispose();
            }
        }
    }
}
