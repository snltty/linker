using linker.libs;
using System.Text;

namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy
    {
        private readonly NumberSpace ns = new NumberSpace();
        private byte[] flagBytes = Encoding.UTF8.GetBytes($"snltty.sforward");

        public SForwardProxy()
        {
            UdpTask();
        }

        public string Start(int port, bool isweb)
        {
            try
            {
                StartTcp(port, isweb);
                StartUdp(port);
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
