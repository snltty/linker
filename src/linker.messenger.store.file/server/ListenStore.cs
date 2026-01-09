using linker.messenger.listen;

namespace linker.messenger.store.file.server
{
    public sealed class ListenStore : IListenStore
    {
        public int Port => config.Data.Server.ServicePort;
        public int ApiPort => config.Data.Server.ApiPort;

        public string[] WhiteCountrys => config.Data.Server.WhiteCountrys;
        public string[] BlackCountrys => config.Data.Server.BlackCountrys;

        private readonly FileConfig config;
        public ListenStore(FileConfig config)
        {
            this.config = config;
        }

        public bool SetPort(int port)
        {
            config.Data.Server.ServicePort = port;
            return true;
        }
        public bool SetApiPort(int port)
        {
            config.Data.Server.ApiPort = port;
            return true;
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }

        public bool SetWhiteCountrys(string[] whiteCountrys)
        {
            config.Data.Server.WhiteCountrys = whiteCountrys;
            return true;
        }
        public bool SetBlackCountrys(string[] blackCountrys)
        {
            config.Data.Server.BlackCountrys = blackCountrys;
            return true;
        }
    }
}
