using common.libs.extends;
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
            new ClientServerInfo{ Name="默认", Host=new IPEndPoint(IPAddress.Loopback, 1802).ToString() }
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

        public string GroupId { get; set; } = "snltty";

        public string ShareMemoryKey { get; set; } = "cmonitor/share";
        public int ShareMemoryCount { get; set; } = 100;
        public int ShareMemorySize { get; set; } = 1024;

        public string Server { get; set; } = new IPEndPoint(IPAddress.Loopback, 1802).ToString();

    }

    public sealed class ClientServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
    }
}
