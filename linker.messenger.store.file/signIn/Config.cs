using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using System.Net;
using System.Text;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigClientInfo : IConfig
    {
        public bool OnlyNode { get; set; }


        private SignInClientServerInfo[] servers = new SignInClientServerInfo[] {
#if DEBUG
            new SignInClientServerInfo{ Name="Linker", Host=new IPEndPoint(IPAddress.Loopback, 1802).ToString() }
#else
            new ClientServerInfo{ Name="Linker", Host="linker.snltty.com:1802" }
#endif
        };
        public SignInClientServerInfo Server => Servers[0];
        public SignInClientServerInfo[] Servers
        {
            get => servers; set { servers = value; }
        }

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

        private SignInClientGroupInfo[] groups = new[] { new SignInClientGroupInfo { } };
        public SignInClientGroupInfo Group => Groups[0];
        public SignInClientGroupInfo[] Groups
        {
            get => groups; set
            {
                groups = value;
                if (groups.Length == 0) groups = new[] { new SignInClientGroupInfo { } };
            }
        }

        /// <summary>
        /// 加密证书
        /// </summary>
        public ClientCertificateInfo SSL { get; set; } = new ClientCertificateInfo();

    }

    public sealed partial class ClientCertificateInfo
    {
        public ClientCertificateInfo() { }
        public string File { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";
    }


    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 登入
        /// </summary>
        public SignInConfigServerInfo SignIn { get; set; } = new SignInConfigServerInfo();
    }

}
