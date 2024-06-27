using linker.libs;
using linker.libs.extends;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;

namespace linker.plugins.tuntap.vea
{
    public sealed class TuntapVeaLinux : ITuntapVea
    {
        private string interfaceLinux = string.Empty;
        private Process Tun2SocksProcess;
        private IPAddress ip;

        public bool Running => string.IsNullOrWhiteSpace(interfaceLinux) == false;
        public string InterfaceName => "linker";
        public string Error { get; private set; }

        public TuntapVeaLinux()
        {
        }

        public async Task<bool> Run(int proxyPort, IPAddress ip)
        {
            CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {InterfaceName}" });
            await SetIp(ip);
            string str = CommandHelper.Linux(string.Empty, new string[] { $"ifconfig" });
            if (str.Contains(InterfaceName) == false)
            {
                Error = CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {InterfaceName}" });
                LoggerHelper.Instance.Error(Error);
                return false;
            }

            interfaceLinux = GetLinuxInterfaceNum();
            try
            {
                string command = $" -device {InterfaceName} -proxy socks5://127.0.0.1:{proxyPort} -interface {interfaceLinux} -loglevel silent";
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"vea linux ->exec:{command}");
                }
                Tun2SocksProcess = CommandHelper.Execute("./plugins/tuntap/tun2socks", command);
                if (Tun2SocksProcess.HasExited)
                {
                    Error = CommandHelper.Execute("./plugins/tuntap/tun2socks", command, Array.Empty<string>());
                    LoggerHelper.Instance.Error(Error);
                }
               
                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                LoggerHelper.Instance.Error(ex.Message);
                return false;
            }

            return string.IsNullOrWhiteSpace(interfaceLinux) == false;
        }
        public async Task<bool> SetIp(IPAddress ip)
        {
            if (this.ip != null)
            {
                CommandHelper.Linux(string.Empty, new string[] { $"ip addr del {this.ip}/24 dev {InterfaceName}" });
            }
            this.ip = ip;
            CommandHelper.Linux(string.Empty, new string[] { $"ip addr add {ip}/24 dev {InterfaceName}", $"ip link set dev {InterfaceName} up" });
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
            foreach (var item in Process.GetProcesses().Where(c => c.ProcessName.Contains("tun2socks")))
            {
                try
                {
                    item.Kill();
                }
                catch (Exception)
                {
                }
            };

            interfaceLinux = string.Empty;
            CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap del mode tun dev {InterfaceName}" });
            Error = string.Empty;
        }
        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip)
        {
            string[] commands = ips.Where(c => c.IPAddress > 0).Select(item =>
            {
                return $"ip route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength} via {ip} dev {InterfaceName} metric 1 ";
            }).ToArray();
            if (commands.Length > 0)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Warning($"vea linux ->add route:{string.Join(Environment.NewLine, commands)}");
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

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"vea linux ->del route:{string.Join(Environment.NewLine, commands)}");
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
