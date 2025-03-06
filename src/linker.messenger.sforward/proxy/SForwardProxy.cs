using linker.libs;
using System.Text;
namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy
    {

        private readonly NumberSpace ns = new NumberSpace();
        private byte[] flagBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.sforward");

        public SForwardProxy()
        {
            UdpTask();

        }

        public virtual void AddReceive(string key,string groupid, long bytes)
        {
        }
        public virtual void AddSendt(string key,string groupid, long bytes)
        {
        }

        public string Start(int port, bool isweb, byte bufferSize,string groupid)
        {
            try
            {
                StartTcp(port, isweb, bufferSize, groupid);
                StartUdp(port, bufferSize, groupid);
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
