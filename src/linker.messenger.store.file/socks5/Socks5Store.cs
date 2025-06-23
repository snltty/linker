using linker.messenger.socks5;

namespace linker.messenger.store.file.socks5
{
    public sealed class Socks5Store : ISocks5Store
    {
        public List<Socks5LanInfo> Lans => runningConfig.Data.Socks5.Lans;

        public int Port => runningConfig.Data.Socks5.Port;

        public bool Running => runningConfig.Data.Socks5.Running;
        public string Error => error;
        private string error = string.Empty;


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

        public void SetRunning(bool running, string error)
        {
            runningConfig.Data.Socks5.Running = running;
            this.error = error;
            runningConfig.Data.Update();
        }

        public bool Confirm()
        {
            runningConfig.Data.Update();
            return true;
        }
    }
}
