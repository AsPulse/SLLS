using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SLLS_Recorder.Recording {
    internal class FFMpegWriterController
    {
        public Dictionary<int, Task<FFMpegWriter>> Writers = new();
        public Func<int, FFMpegWriter> Provider { get; }

        public FFMpegWriterController(Func<int, FFMpegWriter> provider)
        {
            Provider = provider;
        }

        public void Ready(int chunkId)
        {
            if (Writers.ContainsKey(chunkId)) return;
            Writers.Add(
                chunkId,
                Task.Run(() => Provider(chunkId))
            );
            ReportStack();
        }

        public Task<FFMpegWriter> GetVw(int chunkId)
        {
            if (!Writers.ContainsKey(chunkId)) Ready(chunkId);
            return Writers[chunkId];
        }

        public Task RenderChunk(int chunkId)
        {
            return Task.Run(async () =>
            {
                if (!Writers.ContainsKey(chunkId)) return;
                FFMpegWriter target = await Writers[chunkId];
                await target.Render();
                Writers.Remove(chunkId);
                ReportStack();
            });
        }

        public Task FreeChunk(int chunkId)
        {
            return Task.Run(async () =>
            {
                if (!Writers.ContainsKey(chunkId)) return;
                FFMpegWriter target = await Writers[chunkId];
                await target.Free();
                Writers.Remove(chunkId);
                ReportStack();
            });
        }

        public Task FreeAllChunk()
        {
            return Task.WhenAll(Writers.Keys.Select(x => FreeChunk(x)));
        }

        public void ReportStack()
        {
            Debug.WriteLine(string.Format("Now we have {0} writer(s)", Writers.Count));
        }


    }
}
