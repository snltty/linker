using common.libs.extends;
using MemoryPack;
using System.Net;

namespace cmonitor.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
    }

    public sealed partial class ConfigClientInfo
    {
        private ClientServerInfo[] servers = new ClientServerInfo[] {
#if DEBUG
            new ClientServerInfo{ Name="默认", Host=new IPEndPoint(IPAddress.Loopback, 1802).ToString() }
#else
            new ClientServerInfo{ Name="默认", Host="hk.cm.snltty.com:1802" }
#endif
            
        };
        public ClientServerInfo[] Servers
        {
            get => servers; set
            {
                servers = value;
                if (value.Length > 0) Server = value.FirstOrDefault().Host;
            }
        }


        private string name = Dns.GetHostName().SubStr(0, 12);
        public string Name
        {
            get => name; set
            {
                name = value.SubStr(0, 12);
            }
        }

        private string groupid = "snltty";
        public string GroupId
        {
            get => groupid; set
            {
                groupid = value.SubStr(0, 36);
            }
        }

        public string ShareMemoryKey { get; set; } = "cmonitor/share";
        public int ShareMemoryCount { get; set; } = 100;
        public int ShareMemorySize { get; set; } = 1024;
#if DEBUG
        public string Server { get; set; } = new IPEndPoint(IPAddress.Loopback, 1802).ToString();
#else
        public string Server { get; set; } = "hk.cm.snltty.com:1802";
#endif

    }

    [MemoryPackable]
    public sealed partial class ClientServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
    }
}
