using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SLLS_Recorder.Streaming {
    internal class SLLSClientController
    {
        public SynchronizedCollection<SLLSClient> List = new();

        public byte NewDevice(TcpClient client, Server s)
        {
            for (byte i = 0x00; i <= 0xEF; i++)
            {
                if (List.FirstOrDefault(v => v.DeviceId == i) == null)
                {
                    List.Add(new SLLSClient(s, client, i));
                    return i;
                }
            }
            throw new Exception("DeviceId has been exhausted.");
        }

        public void Release(TcpClient client)
        {
            List.Where(v => v.TcpClient == client).ToList().ForEach(v =>
            {
                v.Disconnect();
                List.Remove(v);
            });
        }
    }
}
