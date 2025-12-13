using linker.messenger.node;

namespace linker.messenger.store.file.node
{
    public class NodeConfigStore<TConfig> : INodeConfigStore<TConfig> where TConfig : class, INodeConfigBase, new()
    {
        public int ServicePort => config.Data.Server.ServicePort;
        public virtual TConfig Config { get; }

        private readonly FileConfig config;
        public NodeConfigStore(FileConfig config)
        {
            this.config = config;
        }

        public void Confirm()
        {
            config.Data.Update();
        }

        public void SetDataRemain(long value)
        {
            Config.DataRemain = value;
        } 

        public void SetDataMonth(int month)
        {
            Config.DataMonth = month;
        }

        public void SetShareKey(string shareKey)
        {
            Config.ShareKey = shareKey;
        }

        public void SetMasterKey(string masterKey)
        {
            Config.MasterKey = masterKey;
        }

        public void SetShareKeyManager(string shareKeyManager)
        {
            Config.ShareKeyManager = shareKeyManager;
        }
    }
}
