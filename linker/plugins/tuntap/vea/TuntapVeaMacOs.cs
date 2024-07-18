using linker.libs;
using System.Diagnostics;
using System.Net;

namespace linker.plugins.tuntap.vea
{
    public sealed class TuntapVeaMacOs : ITuntapVea
    {
        private string interfaceOsx = string.Empty;
        private Process Tun2SocksProcess;
        private IPAddress ip;

        public bool Running => string.IsNullOrWhiteSpace(interfaceOsx) == false;
        public string InterfaceName => "utun12138";
        public string Error { get; private set; }

        public TuntapVeaMacOs()
        {
            CommandHelper.Osx(string.Empty, new string[] { $"chmod a+x ./plugins/tuntap/tun2socks" });
        }

        public async Task<bool> Run(int proxyPort, IPAddress ip)
        {
            interfaceOsx = GetOsxInterfaceNum();
            try
            {
                Tun2SocksProcess = CommandHelper.Execute("./plugins/tuntap/tun2socks", $" -device {InterfaceName} -proxy socks5://127.0.0.1:{proxyPort} -interface {interfaceOsx} -loglevel silent");
                if (Tun2SocksProcess.HasExited)
                {
                    Error = CommandHelper.Execute("./plugins/tuntap/tun2socks", $" -device {InterfaceName} -proxy socks5://127.0.0.1:{proxyPort} -interface {interfaceOsx} -loglevel silent", Array.Empty<string>());
                    LoggerHelper.Instance.Error(Error);
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                LoggerHelper.Instance.Error(ex.Message);
                return false;
            }

            for (int i = 0; i < 5; i++)
            {
                string output = CommandHelper.Osx(string.Empty, new string[] { "ifconfig" });
                if (output.Contains(InterfaceName))
                {
                    break;
                }
                await Task.Delay(1000).ConfigureAwait(false);
            }

            await SetIp(ip).ConfigureAwait(false);

            return string.IsNullOrWhiteSpace(interfaceOsx) == false;
        }
        public async Task<bool> SetIp(IPAddress ip)
        {
            if (this.ip != null)
            {
                var ips = this.ip.GetAddressBytes();
                ips[^1] = 0;
                CommandHelper.Osx(string.Empty, new string[] { $"route delete -net {new IPAddress(ips)}/24 {this.ip}" });
            }

            this.ip = ip;
            CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {InterfaceName} {ip} {ip} up" });
            await Task.Delay(10).ConfigureAwait(false);

            var ipBytes = ip.GetAddressBytes();
            ipBytes[^1] = 0;
            CommandHelper.Osx(string.Empty, new string[] { $"route add -net {new IPAddress(ipBytes)}/24 {ip}" });
            return true;
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

            interfaceOsx = string.Empty;
            var ip = this.ip.GetAddressBytes();
            ip[^1] = 0;
            CommandHelper.Osx(string.Empty, new string[] { $"route delete -net {new IPAddress(ip)}/24 {this.ip}" });
            Error = string.Empty;
        }

        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip)
        {
            string[] commands = ips.Where(c => c.IPAddress > 0).Select(item =>
            {
                IPAddress _ip = NetworkHelper.ToNetworkIp(item.IPAddress, item.MaskValue);
                return $"route add -net {_ip}/{item.MaskLength} {ip}";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands.ToArray());
            }
        }
        public void DelRoute(TuntapVeaLanIPAddress[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                IPAddress _ip = NetworkHelper.ToNetworkIp(item.IPAddress, item.MaskValue);
                return $"route delete -net {_ip}/{item.MaskLength}";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands.ToArray());
            }
        }

        private string GetOsxInterfaceNum()
        {
            string output = CommandHelper.Osx(string.Empty, new string[] { "ifconfig" });
            var arr = output.Split(Environment.NewLine);
            for (int i = 0; i < arr.Length; i++)
            {
                var item = arr[i];
                if (item.Contains("inet "))
                {
                    for (int k = i; k >= 0; k--)
                    {
                        var itemk = arr[k];
                        if (itemk.Contains("flags=") && itemk.StartsWith("en"))
                        {
                            return itemk.Split(": ")[0];
                        }
                    }
                }

            }
            return string.Empty;
        }
    }
}
