using linker.libs;

namespace linker.messenger.socks5
{
    public sealed class Socks5Transfer
    {
        public Action OnChanged { get; set; } = () => { };

        private readonly TunnelProxy tunnelProxy;
        private readonly ISocks5Store socks5Store;
        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        public Socks5Transfer( TunnelProxy tunnelProxy, ISocks5Store socks5Store)
        {
            this.tunnelProxy = tunnelProxy;
            this.socks5Store = socks5Store;
            if (socks5Store.Running) Retstart();
            
        }
       
        /// <summary>
        /// 重启
        /// </summary>
        /// <returns></returns>
        public void Retstart()
        {
            tunnelProxy.Start(socks5Store.Port);
            socks5Store.SetRunning(tunnelProxy.Running);
            OnChanged();
        }
        /// <summary>
        /// 网卡
        /// </summary>
        public void Stop()
        {
            tunnelProxy.Stop();
            socks5Store.SetRunning(tunnelProxy.Running);
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
                int port = socks5Store.Port;

                socks5Store.SetPort(info.Port);
                socks5Store.SetLans(info.Lans);

                if ((port != socks5Store.Port && socks5Store.Running) || (socks5Store.Running && tunnelProxy.Running == false))
                {
                    Retstart();
                }
                OnChanged();
            });
        }
    }
}
