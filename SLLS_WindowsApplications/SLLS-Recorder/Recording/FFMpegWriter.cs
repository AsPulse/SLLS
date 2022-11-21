using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SLLS_Recorder.Recording {
    internal class FFMpegWriter
    {
        private readonly Process Proc;
        private readonly StreamWriter sw;

        public int Width { get; }
        public int Height { get; }

        public string OutputPath { get; }

        private readonly Camera camera;
        public readonly long? Id;

        private int AppendedFrames = 0;

        public Dictionary<int, Mat> matTable = new();

        public Task Appender = Task.CompletedTask;

        public static string ZeroLatencyCPU = " -tune fastdecode,zerolatency";
        public static string ZeroLatencyGPU = " -delay 0 -zerolatency 1 -preset llhq -tune ull";
        public FFMpegWriter(Camera camera, string outputPath, long? id)
        {
            Width = camera.width;
            Height = camera.height;
            OutputPath = Path.GetFullPath(outputPath);

            Proc = new Process();
            Proc.StartInfo.FileName = "./ffmpeg.exe";
            Proc.StartInfo.Arguments =
                string.Format(
                    "-y -f rawvideo -pixel_format bgr24 -video_size {0}x{1} -framerate {2}" +
                    " -i - -an -vcodec h264_nvenc -pix_fmt yuv420p" +
                    " -b 5M -maxrate 6M -bufsize 6M" +
                    " -movflags +faststart -flags cgop -qmin 10" +
                    ZeroLatencyGPU +
                    " {3}",
                    Width, Height, camera.fps, OutputPath
                ); ;
            Proc.StartInfo.CreateNoWindow = true;
            Proc.StartInfo.UseShellExecute = false;
            Proc.StartInfo.RedirectStandardInput = true;
            Proc.Start();
            sw = Proc.StandardInput;
            this.camera = camera;
            Id = id;
        }

        public Task AppendFrame(Mat m)
        {
            return Task.Run(() =>
            {
                int size = Width * Height * 3;
                byte[] buffer = new byte[size];
                Marshal.Copy(m.Data, buffer, 0, size);
                sw.BaseStream.Write(buffer);
                sw.Flush();
            });
        }

        public void SetFrame(int frame, Mat mat)
        {
            Appender = Task.WhenAll(
                Appender,
                Task.Run(async () =>
                {
                    if (frame == AppendedFrames)
                    {
                        await AppendFrame(mat);
                        AppendedFrames++;
                        while (matTable.ContainsKey(AppendedFrames))
                        {
                            await AppendFrame(matTable[AppendedFrames]);
                            matTable.Remove(AppendedFrames);
                            AppendedFrames++;
                        }
                        if (frame + 1 < AppendedFrames)
                        {
                            Debug.WriteLine(string.Format("Waited frame ({0}th frame) is provided. Now head on {1}th frame", frame, AppendedFrames));
                        }
                    }
                    else
                    {
                        matTable.Add(frame, mat);
                    }
                })
           );
        }

        public async Task<Chunk?> Render()
        {
            Chunk? chunk = null;
            Stopwatch renderTime = Stopwatch.StartNew();
            await Appender;
            sw.Close();
            await Proc.WaitForExitAsync();
            if (File.Exists(OutputPath)) {
                if (Id != null) {
                    using FileStream sr = File.OpenRead(OutputPath);
                    long length = sr.Length;
                    byte[] bytes = new byte[length];
                    sr.Read(bytes, 0, (int)length);
                    chunk = new Chunk((long)Id, bytes, AppendedFrames / camera.fps * 1000);
                }
                File.Delete(OutputPath);
            }
            Proc.Close();
            Debug.WriteLine(string.Format("Encoded! ({0}ms)", renderTime.ElapsedMilliseconds));
            return chunk;
        }

        public async Task Free()
        {
            Proc.Kill();
            await Task.Delay(1000);
            if (File.Exists(OutputPath)) File.Delete(OutputPath);
        }
    }
}
