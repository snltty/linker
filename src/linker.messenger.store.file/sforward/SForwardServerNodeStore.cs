using linker.messenger.sforward.server;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerNodeStore : ISForwardServerNodeStore
    {
        public int ServicePort => config.Data.Server.ServicePort;
        public SForwardServerNodeInfo Node => config.Data.Server.SForward.Distributed.Node;

        private readonly FileConfig config;
        public SForwardServerNodeStore(FileConfig config)
        {
            this.config = config;
        }

        public void Confirm()
        {
            config.Data.Update();
        }

        public void SetInfo(SForwardServerNodeInfo node)
        {
            config.Data.Server.SForward.Distributed.Node = node;
        }
        public void UpdateInfo(SForwardServerNodeUpdateInfo update)
        {
            config.Data.Server.SForward.Distributed.Node.Name = update.Name;
            config.Data.Server.SForward.Distributed.Node.MaxBandwidth = update.MaxBandwidth;
            config.Data.Server.SForward.Distributed.Node.MaxBandwidthTotal = update.MaxBandwidthTotal;
            config.Data.Server.SForward.Distributed.Node.MaxGbTotal = update.MaxGbTotal;
            config.Data.Server.SForward.Distributed.Node.MaxGbTotalLastBytes = update.MaxGbTotalLastBytes;
            config.Data.Server.SForward.Distributed.Node.Public = update.Public;
            config.Data.Server.SForward.Distributed.Node.Domain = update.Domain;
            config.Data.Server.SForward.Distributed.Node.Host = update.Host;
            config.Data.Server.SForward.Distributed.Node.Url = update.Url;
            config.Data.Server.SForward.Distributed.Node.Sync2Server = update.Sync2Server;

            config.Data.Server.SForward.WebPort = update.WebPort;
            config.Data.Server.SForward.TunnelPortRange = update.PortRange;
        }
        public void SetMasterHosts(string[] hosts)
        {
            config.Data.Server.SForward.Distributed.Node.MasterHosts = hosts;
        }
        public void SetMaxGbTotalLastBytes(long value)
        {
            config.Data.Server.SForward.Distributed.Node.MaxGbTotalLastBytes = value;
        }

        public void SetMaxGbTotalMonth(int month)
        {
            config.Data.Server.SForward.Distributed.Node.MaxGbTotalMonth = month;
        }
    }
}
