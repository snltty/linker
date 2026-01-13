using linker.messenger.listen;

namespace linker.messenger.store.file.server
{
    public sealed class ListenStore : IListenStore
    {
        public int Port => config.Data.Server.ServicePort;
        public int ApiPort => config.Data.Server.ApiPort;

        public GeoRegistryInfo GeoRegistry => config.Data.Server.GeoRegistry;

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
    }
}
