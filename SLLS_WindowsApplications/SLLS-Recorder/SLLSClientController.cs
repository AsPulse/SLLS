using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Recorder {
    internal class SLLSClientController {
        public SynchronizedCollection<SLLSClient> List = new();

        private byte NewDevice(TcpClient client) {
            for(byte i = 0x00; i <= 0xEF; i++) {
                if(List.FirstOrDefault(v => v.DeviceId == i) == null) {
                    List.Add(new SLLSClient(client, i));
                    return i;
                }
            }
            throw new Exception("DeviceId has been exhausted.");
        }
    }
}
