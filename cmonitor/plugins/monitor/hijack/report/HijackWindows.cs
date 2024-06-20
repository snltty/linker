using cmonitor.plugins.hijack.report.hijack;
using common.libs;

namespace cmonitor.plugins.hijack.report
{
    public sealed class HijackWindows : IHijack
    {
        private readonly HijackEventHandler hijackEventHandler;
        private readonly HijackController hijackController;

        public HijackWindows()
        {
            hijackEventHandler = new HijackEventHandler();
            hijackController = new HijackController(hijackEventHandler);
        }

        public ulong UdpSend => hijackEventHandler.UdpSend;
        public ulong UdpReceive => hijackEventHandler.UdpReceive;
        public ulong TcpSend => hijackEventHandler.TcpSend;
        public ulong TcpReceive => hijackEventHandler.TcpReceive;

        public void Start()
        {
            try
            {
                hijackController.Start();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        public void Stop()
        {
            hijackController.Stop();
        }

        public void SetProcess(string[] white, string[] black)
        {
            hijackController.SetProcess(white, black);
        }
        public void SetDomain(string[] white, string[] black, bool kill)
        {
            hijackController.SetDomain(white, black, kill);
        }
        public void SetIP(string[] white, string[] black)
        {
            hijackController.SetIP(white, black);
        }
        public void UpdateRules()
        {
            hijackController.UpdateRules();
        }
    }
}
