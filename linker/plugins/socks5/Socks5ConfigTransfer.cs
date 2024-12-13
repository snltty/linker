using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.socks5.config;

namespace linker.plugins.socks5
{
    public sealed class Socks5ConfigTransfer
    {
        public List<Socks5LanInfo> Lans=> runningConfig.Data.Socks5.Lans;
        public int Port=> runningConfig.Data.Socks5.Port;

        public Action OnChanged { get; set; } = () => { };

        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;
        private readonly TunnelProxy tunnelProxy;
        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        public Socks5ConfigTransfer(FileConfig config, RunningConfig runningConfig, TunnelProxy tunnelProxy)
        {
            this.config = config;
            this.runningConfig = runningConfig;
            this.tunnelProxy = tunnelProxy;

            if (runningConfig.Data.Socks5.Running) Retstart();
            
        }
       
        /// <summary>
        /// 重启
        /// </summary>
        /// <returns></returns>
        public void Retstart()
        {
            tunnelProxy.Start(runningConfig.Data.Socks5.Port);
            runningConfig.Data.Socks5.Running = tunnelProxy.Running;
            runningConfig.Data.Update();
            OnChanged();
        }
        /// <summary>
        /// 网卡
        /// </summary>
        public void Stop()
        {
            tunnelProxy.Stop();
            runningConfig.Data.Socks5.Running = tunnelProxy.Running;
            runningConfig.Data.Update();
            OnChanged();
        }

        /// <summary>
        /// 更新本机信息
        /// </summary>
        /// <param name="info"></param>
        public void UpdateConfig(Socks5Info info)
        {
            TimerHelper.Async(() =>
            {
                int port = runningConfig.Data.Socks5.Port;

                runningConfig.Data.Socks5.Port = info.Port;
                runningConfig.Data.Socks5.Lans = info.Lans;
                runningConfig.Data.Update();

                bool needReboot = (port != runningConfig.Data.Socks5.Port && runningConfig.Data.Socks5.Running)
                || (runningConfig.Data.Socks5.Running && tunnelProxy.Running == false);

                if (needReboot)
                {
                    Retstart();
                }
                OnChanged();
            });
        }
    }
}
