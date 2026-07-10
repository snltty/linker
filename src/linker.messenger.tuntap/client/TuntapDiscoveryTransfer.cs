using linker.discovery;
using linker.libs.extends;

namespace linker.messenger.tuntap.client
{
    public sealed class TuntapDiscoveryTransfer
    {
        private readonly DiscoveryRelayTransfer discoveryRelayTransfer = new DiscoveryRelayTransfer();

        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        public TuntapDiscoveryTransfer(TuntapConfigTransfer tuntapConfigTransfer, TuntapTransfer tuntapTransfer)
        {
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            BuildDefaults();


            tuntapTransfer.OnSetupSuccess += SetupSuccess;
            tuntapTransfer.OnShutdownSuccess += ShutdownSuccess;
        }

        private void BuildDefaults()
        {
            List<DiscoveryProtocolSaveInfo> list = tuntapConfigTransfer.Info.Discoverys;

            if (list == null || list.Count == 0)
            {
                list = DiscoveryProtocolPresets.CreateDefault().Select(c => new DiscoveryProtocolSaveInfo
                {
                    Name = c.Name,
                    Disabled = c.Disabled,
                    LanIps = c.LanIps
                }).ToList();
            }
            else
            {
                list = tuntapConfigTransfer.Info.Discoverys.Concat(DiscoveryProtocolPresets.CreateDefault().Select(c => new DiscoveryProtocolSaveInfo
                {
                    Name = c.Name,
                    Disabled = c.Disabled,
                    LanIps = c.LanIps
                })).DistinctBy(c => c.Name).ToList();
            }
            tuntapConfigTransfer.Info.Discoverys = list;
        }
        public List<DiscoveryProtocolInfo> GetDiscoverys()
        {
            return DiscoveryProtocolPresets.CreateDefault();
        }

        private void SetupSuccess()
        {
            discoveryRelayTransfer.StartRelay(tuntapConfigTransfer.Info.IP, BuildProtocols());
        }
        private List<DiscoveryProtocolInfo> BuildProtocols()
        {
            List<DiscoveryProtocolInfo> result = DiscoveryProtocolPresets.CreateDefault();
            foreach (var protocol in result)
            {
                var config = tuntapConfigTransfer.Info.Discoverys.FirstOrDefault(p => p.Name == protocol.Name);
                if (config != null)
                {
                    protocol.Disabled = config.Disabled;
                    protocol.LanIps = config.LanIps;
                }
            }
            return result;
        }

        private void ShutdownSuccess()
        {
            discoveryRelayTransfer.StopRelay();
        }
    }
}
