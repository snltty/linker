using linker.libs;
using linker.plugins.flow;
using System.Text;

namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy:IFlow
    {
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }
        public string FlowName => "SForward";


        private readonly NumberSpace ns = new NumberSpace();
        private byte[] flagBytes = Encoding.UTF8.GetBytes($"snltty.sforward");

        public SForwardProxy()
        {
            UdpTask();
        }

       

        public string Start(int port, bool isweb,byte bufferSize)
        {
            try
            {
                StartTcp(port, isweb, bufferSize);
                StartUdp(port, bufferSize);
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }


        public void Stop()
        {
            StopTcp();
            StopUdp();
        }
        public void Stop(int port)
        {
            try
            {
                StopTcp(port);
                StopUdp(port);
            }
            catch (Exception)
            {
            }
        }

    }

}
