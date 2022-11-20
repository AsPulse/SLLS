using SLLS_Common.ManagedPayloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common {
    public abstract class ManagedPayload {
        public TCPPayload? Raw { get; set; }

        protected ManagedPayload(TCPPayload? raw = null) {
            Raw = raw;
        }

        public byte DeviceId { get; set; }

        public const byte SERVER_DEVICEID = 0xF0;

        public abstract byte[] ToByte();

        public abstract string ToLogStringSend();
        public abstract string ToLogStringReceive();
    }
}
