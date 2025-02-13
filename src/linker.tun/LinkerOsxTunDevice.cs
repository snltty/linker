using linker.libs;
using linker.libs.extends;
using Microsoft.Win32.SafeHandles;
using System.Net;

namespace linker.tun
{
    internal sealed class LinkerOsxTunDevice : ILinkerTunDevice
    {

        private string name = string.Empty;
        public string Name => name;
        public bool Running => fs != null;

        private FileStream fs = null;
        private IPAddress address;
        private byte prefixLength = 24;
        private SafeFileHandle safeFileHandle;

        private string interfaceOsx = string.Empty;

        public LinkerOsxTunDevice()
        {
           
        }

        public bool Setup(string name,IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
            this.name = name;
            error = string.Empty;

            interfaceOsx = GetOsxInterfaceNum();
            this.address = address;
            this.prefixLength = prefixLength;

            safeFileHandle = File.OpenHandle($"/dev/{Name}", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.Asynchronous);
            fs = new FileStream(safeFileHandle, FileAccess.ReadWrite, 1500);

            IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));
            CommandHelper.Osx(string.Empty, new string[] {
                $"route delete -net {network}/{prefixLength} {address}",
                $"ifconfig {Name} {address} {address} up",
                $"route add -net {network}/{prefixLength} {address}",
            });
            return true;
        }

        public void Shutdown()
        {
            if (fs != null)
            {
                safeFileHandle?.Dispose();

                interfaceOsx = string.Empty;
                fs.Close();
                fs.Dispose();
                fs = null;
            }
            IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(this.prefixLength));
            CommandHelper.Osx(string.Empty, new string[] { $"route delete -net {network}/{prefixLength} {address}" });
        }

        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            string[] commands = ips.Select(item =>
            {
                IPAddress _ip = NetworkHelper.ToNetworkIP(item.Address, item.PrefixLength);
                return $"route add -net {_ip}/{item.PrefixLength} {ip}";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands.ToArray());
            }
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                IPAddress _ip = NetworkHelper.ToNetworkIP(item.Address, item.PrefixLength);
                return $"route delete -net {_ip}/{item.PrefixLength}";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands.ToArray());
            }
        }

        public void SetMtu(int value)
        {
            CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {Name} mtu {value}" });
        }

        public void SetNat(out string error)
        {
            error = string.Empty;
            /*
              # 开启ip转发
              sudo sysctl -w net.ipv4.ip_forward=1
              # 配置NAT转发规则
              # 在/etc/pf.conf文件中添加以下规则,en0是出口网卡，10.26.0.0/24是来源网段
              nat on en0 from 10.26.0.0/24 to any -> (en0)
              # 加载规则
              sudo pfctl -f /etc/pf.conf -e
            CommandHelper.Osx(string.Empty, new string[] {
                "sysctl -w net.ipv4.ip_forward=1",
                "nat on en0 from 10.26.0.0/24 to any -> (en0)",
                "pfctl -f /etc/pf.conf -e",
            });
            */
        }
        public void RemoveNat(out string error)
        {
            error = string.Empty;
        }

        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            return new List<LinkerTunDeviceForwardItem>();
        }
        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
        }
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
        }


        private byte[] buffer = new byte[2 * 1024];
        public ReadOnlyMemory<byte> Read()
        {
            try
            {
                int length = fs.Read(buffer.AsSpan(4));
                length.ToBytes(buffer);
                return buffer.AsMemory(0, length + 4);
            }
            catch (Exception)
            {
            }
            return Helper.EmptyArray;
        }

        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                fs.Write(buffer.Span);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
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

        public void Clear()
        {

        }

        public async Task<bool> CheckAvailable()
        {
            return await Task.FromResult(true);
        }
    }
}
