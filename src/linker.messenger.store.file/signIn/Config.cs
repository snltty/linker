using linker.libs.extends;
using linker.messenger.signin;
using System.Net;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigClientInfo : IConfig
    {
        public bool OnlyNode { get; set; }

        private SignInClientServerInfo[] servers = new SignInClientServerInfo[] {
#if DEBUG
            new SignInClientServerInfo{ Name="Linker", Host=new IPEndPoint(IPAddress.Loopback, 1802).ToString() }
#else
            new SignInClientServerInfo{ Name="Linker", Host="linker.snltty.com:1802" }
#endif
        };

        [SaveJsonIgnore]
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

        private string name = Dns.GetHostName().SubStr(0, 32);
        public string Name
        {
            get => name; set
            {
                name = value.SubStr(0, 32);
            }
        }

        private SignInClientGroupInfo[] groups = new[] { new SignInClientGroupInfo { } };

        [SaveJsonIgnore]
        public SignInClientGroupInfo Group => Groups[0];
        public SignInClientGroupInfo[] Groups
        {
            get => groups; set
            {
                groups = value;
                if (groups.Length == 0) groups = new[] { new SignInClientGroupInfo { } };
            }
        }


    }

    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 登入
        /// </summary>
        public SignInConfigServerInfo SignIn { get; set; } = new SignInConfigServerInfo();
    }

}
