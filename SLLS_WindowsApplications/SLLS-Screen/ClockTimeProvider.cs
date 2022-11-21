using System;
using SLLS_Common;

namespace SLLS_Recorder {
    internal class ClockTimeProvider : ITimeProvider {
        public long Now() {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}
