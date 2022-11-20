using SLLS_Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder {
    internal class ClockTimeProvider : ITimeProvider {
        public long Now() {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }
}
