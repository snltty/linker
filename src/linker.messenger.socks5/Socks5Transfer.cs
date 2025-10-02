using linker.libs.timer;

namespace linker.messenger.socks5
{
    public sealed class Socks5Transfer
    {
        public Action OnChanged { get; set; } = () => { };

        private readonly Socks5Proxy tunnelProxy;
        private readonly ISocks5Store socks5Store;
        private readonly Socks5Decenter socks5Decenter;
        public Socks5Transfer(Socks5Proxy tunnelProxy, ISocks5Store socks5Store, Socks5Decenter socks5Decenter)
        {
            this.tunnelProxy = tunnelProxy;
            this.socks5Store = socks5Store;
            this.socks5Decenter = socks5Decenter;
            if (socks5Store.Running) Retstart();

        }

        /// <summary>
        /// 重启
        /// </summary>
        /// <returns></returns>
        public void Retstart()
        {
            tunnelProxy.Start(socks5Store.Port);
            socks5Store.SetRunning(tunnelProxy.Running, tunnelProxy.Error);
            socks5Decenter.Refresh();
        }
        /// <summary>
        /// 关闭
        /// </summary>
        public void Stop()
        {
            tunnelProxy.Stop();
            socks5Store.SetRunning(tunnelProxy.Running, tunnelProxy.Error);
            socks5Decenter.Refresh();
        }

        /// <summary>
        /// 更新本机信息
        /// </summary>
        /// <param name="info"></param>
        public void UpdateConfig(Socks5Info info)
        {
            TimerHelper.Async(() =>
            {
                int port = socks5Store.Port;

                socks5Store.SetPort(info.Port);
                socks5Store.SetLans(info.Lans);
                socks5Store.Confirm();

                if ((port != socks5Store.Port && socks5Store.Running) || (socks5Store.Running && tunnelProxy.Running == false))
                {
                    Retstart();
                }
                socks5Decenter.Refresh();
            });
        }
    }
}
