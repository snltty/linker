using linker.messenger.sforward.server;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerMasterStore : ISForwardServerMasterStore
    {
        public SForwardServerMasterInfo Master => config.Data.Server.SForward.Distributed.Master;

        private readonly FileConfig config;
        public SForwardServerMasterStore(FileConfig config)
        {
            this.config = config;
        }

        public void SetInfo(SForwardServerMasterInfo info)
        {
            config.Data.Server.SForward.Distributed.Master = info;
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }
    }
}
