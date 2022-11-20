using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace SLLS_Recorder.Recording {
    enum RecordingStatus
    {
        READY,
        PREPARING_TO_START,
        RECORDING,
        PREPARING_TO_FINISH
    };

    internal class Camera : IDisposable
    {

        public delegate void CameraImageRefreshedEventHandler(object sender);
        public event CameraImageRefreshedEventHandler? CameraImageRefreshed;

        public delegate void StatusChangedEventHandler(object sender, RecordingStatus status);
        public event StatusChangedEventHandler? StatusChanged;

        private bool live = true;

        private readonly int width = 1920;
        private readonly int height = 1080;
        private readonly int fps = 24;

        public RecordingStatus status = RecordingStatus.READY;

        private bool recording = false;
        public readonly int chunkLength = 240;
        public int renderedFrame = 0;
        public int chunkId = -1;

        public int dropFrames = 0;

        private readonly FFMpegWriterController vw;
        private VideoCapture? vc;
        readonly Stopwatch sw = new();
        public WriteableBitmap bmp;

        public Camera()
        {
            vw = new(chunkId =>
                new FFMpegWriter(fps, width, height, string.Format("./data/{0}.mp4", chunkId))
            );
            bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
            Task.Run(Worker_DoWork);
        }

        public void SelectCamera(int id)
        {
            vc = null;
            vc = new(id)
            {
                FrameWidth = width,
                FrameHeight = height
            };

            if (!vc.IsOpened())
            {
                MessageBox.Show("Can't use camera.");
                return;
            }
        }

        private void Worker_DoWork()
        {

            Mat frame = new(width, height, MatType.CV_8UC3);

            while (live)
            {
                if (vc == null) continue;
                vc.Read(frame);
                if (frame.Empty()) continue;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WriteableBitmapConverter.ToWriteableBitmap(frame, bmp);
                    CameraImageRefreshed?.Invoke(this);
                });

                /**
                 * Recording
                 */
                if (recording)
                {
                    int nowFrame = (int)Math.Round(sw.Elapsed.TotalSeconds * fps);
                    int skipped = nowFrame - renderedFrame - 1;
                    if (skipped > 0)
                    {
                        dropFrames += skipped;
                        Debug.WriteLine(string.Format("Dropped: {0} ( of {1} )", skipped, dropFrames));
                    }
                    while (renderedFrame < nowFrame && renderedFrame < chunkLength)
                    {
                        vw.GetVw(chunkId).Result.SetFrame(renderedFrame, frame);
                        renderedFrame++;
                    }
                    if (renderedFrame >= chunkLength)
                    {
                        vw.RenderChunk(chunkId++);
                        Task.Run(() =>
                        {
                            Enumerable.Range(chunkId, 5).ToList().ForEach(x => vw.Ready(x));
                        });
                        renderedFrame = 0;
                        sw.Restart();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (!live) return;
            live = false;
            StopRecord().Wait();
        }

        private void SetStatus(RecordingStatus newStatus)
        {
            status = newStatus;
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusChanged?.Invoke(this, newStatus);
            });
        }


        public void StartRecord()
        {
            if (status != RecordingStatus.READY) return;
            SetStatus(RecordingStatus.PREPARING_TO_START);
            chunkId++;
            renderedFrame = 0;
            dropFrames = 0;
            Task.Run(async () =>
            {
                await Task.WhenAll(Enumerable.Range(chunkId, 5).Select(x => vw.GetVw(x)));
                sw.Restart();
                recording = true;
                Debug.WriteLine("Recording Start");
                SetStatus(RecordingStatus.RECORDING);
            });
        }

        public Task StopRecord()
        {
            if (status != RecordingStatus.RECORDING) return Task.CompletedTask;
            SetStatus(RecordingStatus.PREPARING_TO_FINISH);
            recording = false;
            renderedFrame = 0;
            int finishChunk = chunkId;
            return Task.Run(async () =>
            {
                await vw.RenderChunk(finishChunk);
                await vw.FreeAllChunk();
                SetStatus(RecordingStatus.READY);
            });
        }
    }
}
