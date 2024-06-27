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
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1000);
                    if (Tun2SocksProcess.HasExited)
                    {
                        break;
                    }
                    if (GetWindowsHasInterface(InterfaceName) == false)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Warning($"ipconfig command not found or interface not found");
                        }
                        continue;
                    }
                    

                    interfaceNumber = GetWindowsInterfaceNum();
                    if (interfaceNumber == 0)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Warning($"ipconfig command not found or  interface num not found");
                        }
                        continue;
                    }
                    await SetIp(ip);
                    if (await GetWindowsHasRoute(ip) == false)
                    {
                        LoggerHelper.Instance.Warning($"route command not found or  route ip not found");
                        Kill();
                        Tun2SocksProcess = CommandHelper.Execute("./plugins/tuntap/tun2socks.exe", command);
                        await Task.Delay(10000);
                        continue;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }

            if (interfaceNumber <= 0)
            {
                string msg = CommandHelper.Execute("./plugins/tuntap/tun2socks.exe", command, Array.Empty<string>());
                LoggerHelper.Instance.Error(msg);
            }

            return interfaceNumber > 0;
        }
        public async Task<bool> SetIp(IPAddress ip)
        {
            if (interfaceNumber > 0)
            {
                CommandHelper.Windows(string.Empty, new string[] { $"netsh interface ip set address name=\"{InterfaceName}\" source=static addr={ip} mask=255.255.255.0 gateway=none" });
                for (int k = 0; k < 5; k++)
                {
                    if (GetWindowsHasIp(ip))
                    {
                        return true;
                    }
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error($"vea windows ->set ip fail");
                    }
                    await Task.Delay(500);
                }
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
        }
        public void AddRoute(TuntapVeaLanIPAddress[] ips, IPAddress ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ips.Where(c => c.IPAddress > 0).Select(item =>
                {
                    byte[] maskArr = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(item.MaskValue));
                    return $"route add {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())} mask {string.Join(".", maskArr)} {ip} metric 5 if {interfaceNumber}";
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
                string[] commands = ip.Where(c => c.IPAddress > 0).Select(item => $"route delete {string.Join(".", BinaryPrimitives.ReverseEndianness(item.IPAddress).ToBytes())}").ToArray();
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


        private int GetWindowsInterfaceNum()
        {
            string output = CommandHelper.Windows(string.Empty, new string[] { "route print" });
            foreach (var item in output.Split(Environment.NewLine))
            {
                if (item.Contains("WireGuard Tunnel"))
                {
                    return int.Parse(item.Substring(0, item.IndexOf('.')).Trim());
                }
            }
            return 0;
        }
        private bool GetWindowsHasInterface(string name)
        {
            string output = CommandHelper.Windows(string.Empty, new string[] { $"ipconfig | findstr \"{name}\"" });
            if(output.Contains("Windows IP") == false)
            {
                return false;
            }
            return string.IsNullOrWhiteSpace(output) == false;
        }
        private bool GetWindowsHasIp(IPAddress ip)
        {
            string output = CommandHelper.Windows(string.Empty, new string[] { $"ipconfig | findstr \"{ip}\"" });
            return string.IsNullOrWhiteSpace(output) == false;
        }
        private async Task<bool> GetWindowsHasRoute(IPAddress ip)
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(2000);
                string output = CommandHelper.Windows(string.Empty, new string[] { "route print" });
                if (output.Contains(ip.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
