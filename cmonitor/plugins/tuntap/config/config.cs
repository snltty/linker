using cmonitor.plugins.tuntap.config;
using System.Net;

namespace cmonitor.plugins.tuntap.config
{
    public sealed class TuntapConfigInfo
    {
        public IPAddress IP { get; set; } = IPAddress.Any;
        public IPAddress[] LanIPs { get; set; } = Array.Empty<IPAddress>();
    }
    public sealed class TuntapRunningConfigInfo
    {
        public bool Running { get; set; }
    }
}


namespace cmonitor.config
{
    public sealed partial class ConfigClientInfo
    {
        public TuntapConfigInfo Tuntap { get; set; } = new TuntapConfigInfo();
    }
}

namespace cmonitor.client.running
{
    public sealed partial class RunningConfigInfo
    {
        private TuntapRunningConfigInfo tuntap = new TuntapRunningConfigInfo();
        public TuntapRunningConfigInfo Tuntap
        {
            get => tuntap; set
            {
                Updated++;
                tuntap = value;
            }
        }
    }
}