using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common {
    public class SLLSListener : TcpListener {
        public new bool Active => base.Active;

        public SLLSListener(IPEndPoint ep) : base(ep) { }
    }
}
