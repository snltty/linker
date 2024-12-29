using linker.messenger.socks5;

namespace linker.messenger.store.file.socks5
{
    public sealed class Socks5Store : ISocks5Store
    {
        public List<Socks5LanInfo> Lans => runningConfig.Data.Socks5.Lans;

        public int Port => runningConfig.Data.Socks5.Port;

        public bool Running => runningConfig.Data.Socks5.Running;

        private readonly RunningConfig runningConfig;
        public Socks5Store(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
        }

        public void SetLans(List<Socks5LanInfo> lans)
        {
            runningConfig.Data.Socks5.Lans = lans;
            runningConfig.Data.Update();
        }

        public void SetPort(int port)
        {
            runningConfig.Data.Socks5.Port = port;
            runningConfig.Data.Update();
        }

        public void SetRunning(bool running)
        {
            runningConfig.Data.Socks5.Running = running;
            runningConfig.Data.Update();
        }
    }
}
