using System.Net;
using System.Net.Sockets;

namespace SLLS_Common {
    public class SLLSListener : TcpListener {
        public new bool Active => base.Active;

        public SLLSListener(IPEndPoint ep) : base(ep) { }
    }
}
