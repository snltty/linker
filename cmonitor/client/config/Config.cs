using cmonitor.config;
using common.libs;
using common.libs.extends;
using LiteDB;
using MemoryPack;
using System.Net;

namespace cmonitor.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public ClientRunningInfo Client { get; set; } = new ClientRunningInfo();
    }

    public sealed class ClientRunningInfo
    {
        public ObjectId Id { get; set; }

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
            }
        }
    }
}

namespace cmonitor.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
    }

    public sealed partial class ConfigClientInfo
    {

#if DEBUG
        public string Server { get; set; } = new IPEndPoint(IPAddress.Loopback, 1802).ToString();
#else
        public string Server { get; set; } = "hk.cm.snltty.com:1802";
#endif

        private string id = string.Empty;
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
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

        private string groupid = Helper.GlobalString;
        public string GroupId
        {
            get => groupid; set
            {
                groupid = value.SubStr(0, 36);
            }
        }

        public string Certificate { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";

        public bool Installed { get; set; } = false;

        public ConfigClientInfo Load(string text)
        {
            return text.DeJson<ConfigClientInfo>();
        }

    }

    [MemoryPackable]
    public sealed partial class ClientServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
    }
}
