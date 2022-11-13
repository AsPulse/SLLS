using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SLLS_Recorder {
    internal class FFMpegWriter {
        private readonly Process Proc;
        private readonly StreamWriter sw;

        public int Width { get; }
        public int Height { get; }

        public string OutputPath { get; }

        private int AppendedFrames = 0;

        public Dictionary<int, Mat> matTable = new();

        public Task Appender = Task.CompletedTask;
        public FFMpegWriter(int framerate, int width, int height, string outputPath) {
            Width = width;
            Height = height;
            OutputPath = Path.GetFullPath(outputPath);

            Proc = new Process();
            Proc.StartInfo.FileName = "ffmpeg";
            Proc.StartInfo.Arguments =
                string.Format(
                    "-y -f rawvideo -pixel_format bgr24 -video_size {0}x{1} -framerate {2}" + 
                    " -i - -an -vcodec h264_nvenc -pix_fmt yuv420p" + 
                    " -crf 20 -maxrate 8M -bufsize 8M" +
                    " -movflags +faststart -flags cgop -qmin 10" +
                    " -b_strategy 0 -bf 0 -keyint_min 1 -force_key_frames expr:gte(n,n_forced*12)" + 
                    " {3}",
                    width, height, framerate, OutputPath
                );
            Proc.StartInfo.CreateNoWindow = true;
            Proc.StartInfo.UseShellExecute = false;
            Proc.StartInfo.RedirectStandardInput = true;
            Proc.StartInfo.RedirectStandardOutput = true;
            Proc.StartInfo.RedirectStandardError = true;
            Proc.Start();
            sw = Proc.StandardInput;
        }

        public Task AppendFrame(Mat m) {
            return Task.Run(() => {
                int size = Width * Height * 3;
                byte[] buffer = new byte[size];
                Marshal.Copy(m.Data, buffer, 0, size);
                try {
                    sw.BaseStream.Write(buffer);
                } catch {
                    Debug.WriteLine(Proc.StandardOutput.ReadToEnd());
                    Debug.WriteLine(Proc.StandardError.ReadToEnd());
                    throw;
                }
                sw.Flush();
            });
        }

        public void SetFrame(int frame, Mat mat) {
            Appender = Task.WhenAll(
                Appender,
                Task.Run(async () => {
                    if (frame == AppendedFrames) {
                        await AppendFrame(mat);
                        AppendedFrames++;
                        while (matTable.ContainsKey(AppendedFrames)) {
                            await AppendFrame(matTable[AppendedFrames]);
                            AppendedFrames++;
                        }
                        if(frame + 1 < AppendedFrames) {
                            Debug.WriteLine(string.Format("Waited frame ({0}th frame) is provided. Now head on {1}th frame", frame, AppendedFrames));
                        }
                    } else {
                        matTable.Add(frame, mat);
                    }
                })
           );
        }

        public Task Render() {
            return Task.Run(async () => {
                await Appender;
                Stopwatch renderTime = Stopwatch.StartNew();
                sw.Close();
                await Proc.WaitForExitAsync();
                Proc.Close();
                Debug.WriteLine(string.Format("Encoded! ({0}ms)", renderTime.ElapsedMilliseconds));
                //Debug.WriteLine(Proc.StandardOutput.ReadToEnd());
                //Debug.WriteLine(Proc.StandardError.ReadToEnd());
            });
        }

        public Task Free() {
            return Task.Run(async () => {
                Proc.Kill();
                await Task.Delay(1000);
                if (File.Exists(OutputPath)) File.Delete(OutputPath);
            });
        }
    }
}
