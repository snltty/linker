using linker.libs;
using linker.messenger.sforward.server;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardServerConfigStore : ISForwardServerConfigStore
    {
        public int ServicePort => config.Data.Server.ServicePort;
        public SForwardServerConfigInfo Config => config.Data.Server.SForward;

        private readonly FileConfig config;
        public SForwardServerConfigStore(FileConfig config)
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

                Config.Host = NetworkHelper.GetEndPoint(Config.Distributed.Node.Host, ServicePort).Address.ToString();

                Config.Distributed.Node.Id = string.Empty;
                config.Data.Update();
            }
        }

        public void Confirm()
        {
            config.Data.Update();
        }

        public void SetInfo(SForwardServerConfigInfo node)
        {
            config.Data.Server.SForward = node;
        }
        public void SetDataRemain(long value)
        {
            config.Data.Server.SForward.DataRemain = value;
        }

        public void SetDataMonth(int month)
        {
            config.Data.Server.SForward.DataMonth = month;
        }

        public void SetShareKey(string shareKey)
        {
            config.Data.Server.SForward.ShareKey = shareKey;
        }

        public void SetMasterKey(string masterKey)
        {
            config.Data.Server.SForward.MasterKey = masterKey;
        }
    }
}
