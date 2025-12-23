using linker.libs;
using linker.messenger.sforward.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerConfigStore : NodeConfigStore<SForwardServerConfigInfo>, ISForwardNodeConfigStore
    {
        public override SForwardServerConfigInfo Config => config.Data.Server.SForward;

        private readonly FileConfig config;
        public SForwardServerConfigStore(FileConfig config) : base(config)
        {
            this.config = config;
            if (string.IsNullOrWhiteSpace(Config.Distributed.Node.Id) == false)
            {
                Config.NodeId = Config.Distributed.Node.Id;
                Config.DataRemain = Config.Distributed.Node.MaxGbTotalLastBytes;
                Config.DataMonth = Config.Distributed.Node.MaxGbTotalMonth;
                Config.Bandwidth = (int)Config.Distributed.Node.MaxBandwidthTotal;
                Config.DataEachMonth = (int)Config.Distributed.Node.MaxGbTotal;
                Config.Name = Config.Distributed.Node.Name;
                Config.Url = Config.Distributed.Node.Url;

                var ep = NetworkHelper.GetEndPoint(Config.Distributed.Node.Host, ServicePort);
                if (ep != null)
                    Config.Host = ep.Address.ToString();

                Config.Distributed.Node.Id = string.Empty;
                Confirm();
            }
        }

    }
}
