using linker.libs;
using linker.messenger.node;
using linker.messenger.sforward.server;
using System.Text;
namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy
    {

        private readonly NumberSpace ns = new NumberSpace(65537);
        private byte[] flagBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.sforward");

        private readonly SForwardServerNodeTransfer sforwardServerNodeTransfer;
        public SForwardProxy(SForwardServerNodeTransfer sforwardServerNodeTransfer)
        {
            this.sforwardServerNodeTransfer = sforwardServerNodeTransfer;
            UdpTask();
           
        }

        public virtual void Add(string key, string groupid, long recvBytes, long sentBytes)
        {
        }

        public string Start(int port, byte bufferSize, string groupid, bool super,double bandwidth)
        {
            try
            {
                TrafficCacheInfo sForwardTrafficCacheInfo = sforwardServerNodeTransfer.AddTrafficCache(super, bandwidth);
                StartTcp(port, false, bufferSize, groupid, sForwardTrafficCacheInfo);
                StartUdp(port, bufferSize, groupid, sForwardTrafficCacheInfo);
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public string StartHttp(int port, byte bufferSize)
        {
            try
            {
                StartTcp(port, true, bufferSize, "3494B7B2-1D9E-4DA2-B4F7-8C439EB03912", null);
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
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
