using linker.libs;
using linker.libs.extends;
using MemoryPack;
using System.Net;
using System.Text;

namespace linker.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
    }

    public sealed partial class ConfigClientInfo : IConfig
    {
        private ICrypto crypto;
        public ConfigClientInfo()
        {
            crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

            accesss?.Clear();
        }

        public bool OnlyNode { get; set; }


        private ClientServerInfo[] servers = new ClientServerInfo[] {
#if DEBUG
            new ClientServerInfo{ Name="Linker", Host=new IPEndPoint(IPAddress.Loopback, 1802).ToString() }
#else
            new ClientServerInfo{ Name="Linker", Host="linker.snltty.com:1802" }
#endif
        };
        public ClientServerInfo[] Servers
        {
            get => servers; set { servers = value; }
        }
        public ClientServerInfo ServerInfo => servers[0];


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
        private string groupid = Helper.GlobalString;
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


        public ClientGroupInfo Group => Groups.Length == 0 ? new ClientGroupInfo { } : Groups[0];
        public ClientGroupInfo[] Groups { get; set; } = Array.Empty<ClientGroupInfo>();

        /// <summary>
        /// 加密证书
        /// </summary>
        public ClientCertificateInfo SSL { get; set; } = new ClientCertificateInfo();

        public string Serialize(object obj)
        {
#if DEBUG
            return obj.ToJsonFormat();
#else
            return Convert.ToBase64String(crypto.Encode(Encoding.UTF8.GetBytes(obj.ToJson())));
#endif
        }
        public object Deserialize(string text)
        {
            if (text.Contains("ApiPassword"))
            {
                return text.DeJson<ConfigClientInfo>();
            }
            return Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(text)).ToArray()).DeJson<ConfigClientInfo>();
        }

    }

    [MemoryPackable]
    public sealed partial class ClientGroupInfo
    {
        public ClientGroupInfo() { }

        public string Name { get; set; } = string.Empty;

#if DEBUG
        private string id = Helper.GlobalString;
#else
        private string id = string.Empty;
#endif
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
            }
        }

        private string passord = string.Empty;
        public string Password
        {
            get => passord; set
            {
                passord = value.SubStr(0, 36);
            }
        }
    }


    [MemoryPackable]
    public sealed partial class ClientCertificateInfo
    {
        public ClientCertificateInfo() { }
        public string File { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";
    }


    [MemoryPackable]
    public sealed partial class ClientServerInfo
    {
        public ClientServerInfo() { }
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;

    }

}
