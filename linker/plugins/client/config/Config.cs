using linker.config;
using linker.libs;
using linker.libs.extends;
using LiteDB;
using MemoryPack;
using System.Net;
using System.Text;

namespace linker.client.config
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
            new ClientServerInfo{ Name="默认", Host="linker.snltty.com:1802" }
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

namespace linker.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
    }

    public sealed partial class ConfigClientInfo
    {
        private ICrypto crypto;
        public ConfigClientInfo()
        {
            crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);
        }

        public bool OnlyNode {  get; set; }

#if DEBUG
        public string Server { get; set; } = new IPEndPoint(IPAddress.Loopback, 1802).ToString();
#else
        public string Server { get; set; } = "linker.snltty.com:1802";
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
#if DEBUG
        private string groupid = "snltty";
#else
        private string groupid = string.Empty;
#endif
        public string GroupId
        {
            get => groupid; set
            {
                groupid = value.SubStr(0, 36);
            }
        }

        public string Certificate { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";

        public ConfigClientInfo Load(string text)
        {
            if (text.Contains("ApiPassword"))
            {
                return text.DeJson<ConfigClientInfo>();
            }
            return Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(text)).ToArray()).DeJson<ConfigClientInfo>();
        }

        public string Set(object obj)
        {
            return Convert.ToBase64String(crypto.Encode(Encoding.UTF8.GetBytes(obj.ToJson())));
        }

    }

    [MemoryPackable]
    public sealed partial class ClientServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
    }
}
