using linker.messenger.listen;

namespace linker.messenger.store.file.server
{
    public sealed class ListenStore: IListenStore
    {
        public int Port => config.Data.Server.ServicePort;

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

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }
    }
}
