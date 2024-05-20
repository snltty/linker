using common.libs;
using common.libs.extends;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;

namespace cmonitor.plugins.tuntap.vea
{
    public sealed class TuntapVeaLinux : ITuntapVea
    {
        private string interfaceLinux = string.Empty;
        private Process Tun2SocksProcess;
        private const string veaName = "cmonitor";
        private IPAddress ip;

        public bool Running => string.IsNullOrWhiteSpace(interfaceLinux) == false;

        public TuntapVeaLinux()
        {
        }

        public async Task<bool> Run(int proxyPort, IPAddress ip)
        {
            CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {veaName}" });
            await SetIp(ip);
            string str = CommandHelper.Linux(string.Empty, new string[] { $"ifconfig" });
            if (str.Contains(veaName) == false)
            {
                string msg = CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {veaName}" });
                Logger.Instance.Error(msg);
                return false;
            }

            interfaceLinux = GetLinuxInterfaceNum();
            try
            {
                string command = $" -device {veaName} -proxy socks5://127.0.0.1:{proxyPort} -interface {interfaceLinux} -loglevel silent";
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Warning($"vea linux ->exec:{command}");
                }
                Tun2SocksProcess = CommandHelper.Execute("./plugins/tuntap/tun2socks", command);
                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                return false;
            }

            return string.IsNullOrWhiteSpace(interfaceLinux) == false;
        }
        public async Task<bool> SetIp(IPAddress ip)
        {
            if (this.ip != null)
            {
                CommandHelper.Linux(string.Empty, new string[] { $"ip addr del {this.ip}/24 dev {veaName}" });
            }
            this.ip = ip;
            CommandHelper.Linux(string.Empty, new string[] { $"ip addr add {ip}/24 dev {veaName}", $"ip link set dev {veaName} up" });
            return await Task.FromResult(true);
        }

        public void Kill()
        {
            if (Tun2SocksProcess != null)
            {
                try
                {
                    Tun2SocksProcess.Kill();
                }
                catch (Exception)
                {
                }
                Tun2SocksProcess = null;
            }
            interfaceLinux = string.Empty;
            CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap del mode tun dev {veaName}" });
        }
        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip)
        {
            string[] commands = ips.Where(c => c.IPAddress > 0).Select(item =>
            {
                return $"ip route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength} via {ip} dev {veaName} metric 1 ";
            }).ToArray();
            if (commands.Length > 0)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Warning($"vea linux ->add route:{string.Join(Environment.NewLine, commands)}");
                }
                CommandHelper.Linux(string.Empty, commands);
            }
        }
        public void DelRoute(TuntapVeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                return $"ip route del {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength}";
            }).ToArray();

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Logger.Instance.Warning($"vea linux ->del route:{string.Join(Environment.NewLine, commands)}");
            }
            CommandHelper.Linux(string.Empty, commands);
        }

        private string GetLinuxInterfaceNum()
        {
            string output = CommandHelper.Linux(string.Empty, new string[] { "ip route" });
            foreach (var item in output.Split(Environment.NewLine))
            {
                if (item.StartsWith("default via"))
                {
                    var strs = item.Split(' ');
                    for (int i = 0; i < strs.Length; i++)
                    {
                        if (strs[i] == "dev")
                        {
                            return strs[i + 1];
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}
