using common.libs;
using common.libs.extends;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;

namespace cmonitor.plugins.tuntap.vea
{
    public sealed class TuntapVeaMacOs : ITuntapVea
    {
        private string interfaceOsx = string.Empty;
        private Process Tun2SocksProcess;
        private const string veaNameOsx = "utun12138";
        private IPAddress ip;

        public bool Running => string.IsNullOrWhiteSpace(interfaceOsx) == false;

        public TuntapVeaMacOs()
        {
        }

        public async Task<bool> Run(int proxyPort)
        {
            interfaceOsx = GetOsxInterfaceNum();
            try
            {
                Tun2SocksProcess = CommandHelper.Execute("./plugins/tuntap/tun2socks-osx", $" -device {veaNameOsx} -proxy socks5://127.0.0.1:{proxyPort} -interface {interfaceOsx} -loglevel silent");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                return false;
            }

            for (int i = 0; i < 5; i++)
            {
                string output = CommandHelper.Osx(string.Empty, new string[] { "ifconfig" });
                if (output.Contains(veaNameOsx))
                {
                    break;
                }
                await Task.Delay(1000);
            }

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
            CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {veaNameOsx} {ip} {ip} up" });
            await Task.Delay(10);

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
            interfaceOsx = string.Empty;
            var ip = this.ip.GetAddressBytes();
            ip[^1] = 0;
            CommandHelper.Osx(string.Empty, new string[] { $"route delete -net {new IPAddress(ip)}/24 {this.ip}" });
        }

        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip)
        {
            string[] commands = ips.Where(c => c.IPAddress > 0).Select(item =>
            {
                return $"route add -net {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength} {ip}";
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
                return $"route delete -net {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}/{item.MaskLength}";
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
