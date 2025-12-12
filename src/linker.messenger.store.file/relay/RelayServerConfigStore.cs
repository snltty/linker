using linker.libs;
using linker.messenger.relay.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerConfigStore : NodeConfigStore<RelayServerConfigInfo>, IRelayNodeConfigStore
    {
        public override RelayServerConfigInfo Config => config.Data.Server.Relay;

        private readonly FileConfig config;
        public RelayServerConfigStore(FileConfig config) : base(config)
        {
            {
                this.config = config;

                if (string.IsNullOrWhiteSpace(Config.Distributed.Node.Id) == false)
                {
                    Config.NodeId = Config.Distributed.Node.Id;
                    Config.Connections = Config.Distributed.Node.MaxConnection;
                    Config.DataRemain = Config.Distributed.Node.MaxGbTotalLastBytes;
                    Config.DataMonth = Config.Distributed.Node.MaxGbTotalMonth;
                    Config.Bandwidth = (int)Config.Distributed.Node.MaxBandwidthTotal;
                    Config.DataEachMonth = (int)Config.Distributed.Node.MaxGbTotal;
                    Config.Name = Config.Distributed.Node.Name;
                    Config.Url = Config.Distributed.Node.Url;

                    Config.Host = NetworkHelper.GetEndPoint(Config.Distributed.Node.Host, ServicePort).Address.ToString();

                    Config.Distributed.Node.Id = string.Empty;
                    Confirm();
                }
            }

        }
    }
}
