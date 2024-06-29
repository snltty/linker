using linker.libs;
using linker.libs.extends;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;

namespace linker.plugins.tuntap.vea
{
    public sealed class TuntapVeaWindows : ITuntapVea
    {
        private int interfaceNumber = 0;
        private Process Tun2SocksProcess;

        public bool Running => interfaceNumber > 0;
        public string InterfaceName => "linker";
        public string Error { get; private set; }


        public TuntapVeaWindows()
        {
        }

        public async Task<bool> Run(int proxyPort, IPAddress ip)
        {
            string command = $" -device {InterfaceName} -proxy socks5://127.0.0.1:{proxyPort} -loglevel silent";
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"vea windows ->exec:{command}");
            }
            try
            {
                Tun2SocksProcess = CommandHelper.Execute("./plugins/tuntap/tun2socks.exe", command);
                if (Tun2SocksProcess.HasExited)
                {
                    Kill();
                    return false;
                }
                if (await GetWindowsHasInterface(InterfaceName) == false)
                {
                    Kill();
                    return false;
                }
                if (await GetWindowsInterfaceNum() == false)
                {
                    Kill();
                    return false;
                }
                if (await SetIp(ip) == false)
                {
                    Kill();
                    return false;
                }

                if (await GetWindowsHasRoute(ip) == false)
                {
                    Kill();
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }

            if (interfaceNumber <= 0)
            {
                Error = CommandHelper.Execute("./plugins/tuntap/tun2socks.exe", command, Array.Empty<string>());
                LoggerHelper.Instance.Error(Error);
            }
            return interfaceNumber > 0;
        }
        public async Task<bool> SetIp(IPAddress ip)
        {
            if (interfaceNumber == 0) return false;
            CommandHelper.Windows(string.Empty, new string[] { $"netsh interface ip set address name=\"{InterfaceName}\" source=static addr={ip} mask=255.255.255.0 gateway=none" });
            for (int k = 0; k < 5; k++)
            {
                string output = CommandHelper.Windows(string.Empty, new string[] { $"ipconfig" });
                if (output.Contains("Windows IP") == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Error = $"ipconfig command not found";
                        LoggerHelper.Instance.Error(Error);
                    }
                    return false;
                }
                if (output.Contains(ip.ToString()))
                {
                    return true;
                }
                await Task.Delay(1000);
            }
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Error = $"vea windows ->set ip fail";
                LoggerHelper.Instance.Error(Error);
            }
            return false;
        }


        public void Kill()
        {
            interfaceNumber = 0;
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
            Error = string.Empty;
        }
        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ips.Where(c => c.IPAddress > 0).Select(item =>
                {
                    byte[] maskArr = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(item.MaskValue));
                    byte[] ips = BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes();
                    ips[3] = 0;
                    return $"route add {string.Join(".", ips)} mask {string.Join(".", maskArr)} {ip} metric 5 if {interfaceNumber}";
                }).ToArray();
                if (commands.Length > 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"vea windows ->add route:{string.Join(Environment.NewLine, commands)}");
                    }
                    CommandHelper.Windows(string.Empty, commands);
                }
            }
        }
        public void DelRoute(TuntapVeaLanIPAddress[] ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ip.Where(c => c.IPAddress > 0).Select(item =>
                {
                    byte[] ips = BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes();
                    ips[3] = 0;
                    return $"route delete {string.Join(".", ips)}";
                }).ToArray();
                if (commands.Length > 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"vea windows ->del route:{string.Join(Environment.NewLine, commands)}");
                    }
                    CommandHelper.Windows(string.Empty, commands.ToArray());
                }
            }
        }


        private async Task<bool> GetWindowsInterfaceNum()
        {
            for (int i = 0; i < 10; i++)
            {
                string output = CommandHelper.Windows(string.Empty, new string[] { "route print" });
                if (output.Contains("IPv4") == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Error = $"route command not found";
                        LoggerHelper.Instance.Error(Error);
                    }
                    return false;
                }
                foreach (var item in output.Split(Environment.NewLine))
                {
                    if (item.Contains("WireGuard Tunnel"))
                    {
                        interfaceNumber = int.Parse(item.Substring(0, item.IndexOf('.')).Trim());
                        return true;
                    }
                }
                await Task.Delay(1000);
            }
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Error = $"interface number not found";
                LoggerHelper.Instance.Error(Error);
            }
            return false;
        }
        private async Task<bool> GetWindowsHasInterface(string name)
        {
            for (int i = 0; i < 10; i++)
            {
                string output = CommandHelper.Windows(string.Empty, new string[] { $"ipconfig" });
                if (output.Contains("Windows IP") == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Error = $"ipconfig command not found";
                        LoggerHelper.Instance.Error(Error);
                    }
                    return false;
                }
                if (output.Contains(name))
                {
                    return true;
                }
                await Task.Delay(1000);
            }
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Error = $"interface {name} not found";
                LoggerHelper.Instance.Error(Error);
            }
            return false;
        }
        private async Task<bool> GetWindowsHasRoute(IPAddress ip)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                string output = CommandHelper.Windows(string.Empty, new string[] { "route print" });
                if (output.Contains("IPv4") == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Error = $"route command not found";
                        LoggerHelper.Instance.Error(Error);
                    }
                    break;
                }
                if (output.Contains(ip.ToString()))
                {
                    return true;
                }
            }
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Error = $"interface route set fail";
                LoggerHelper.Instance.Error(Error);
            }
            return false;
        }

    }
}
