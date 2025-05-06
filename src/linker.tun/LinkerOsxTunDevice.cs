using linker.libs;
using linker.libs.extends;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Runtime.InteropServices;

namespace linker.tun
{
    /// <summary>
    /// osx网卡实现，未测试
    /// </summary>
    internal sealed class LinkerOsxTunDevice : ILinkerTunDevice
    {

        private string name = string.Empty;
        public string Name => name;
        public bool Running => safeFileHandle != null;

        private FileStream fsRead = null;
        private FileStream fsWrite = null;
        private IPAddress address;
        private byte prefixLength = 24;
        private SafeFileHandle safeFileHandle;

        private string interfaceOsx = string.Empty;

        public LinkerOsxTunDevice()
        {

        }

        public bool Setup(string name, IPAddress address, byte prefixLength, out string error)
        {
            this.name = "utun0";
            error = string.Empty;

            this.address = address;
            this.prefixLength = prefixLength;

            IntPtr arg = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(arg, 0);
            try
            {
                interfaceOsx = GetOsxInterfaceNum();
                safeFileHandle = File.OpenHandle($"/dev/utun", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.Asynchronous);

                int ret = OsxAPI.Ioctl(safeFileHandle, arg);
                if (ret < 0)
                {
                    Shutdown();
                    error = $"open utun failed: {Marshal.GetLastWin32Error()}";
                    return false;
                }
                this.name = $"utun{Marshal.ReadInt32(arg)}";

                fsRead = new FileStream(safeFileHandle, FileAccess.Read, 65 * 1024, true);
                fsWrite = new FileStream(safeFileHandle, FileAccess.Write, 65 * 1024, true);

                IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));

                CommandHelper.Osx(string.Empty, new string[] {
                    $"route delete -net {network}/{prefixLength} {address}",
                    $"ifconfig {Name} {address} {address} up",
                    $"route add -net {network}/{prefixLength} {address}",
                });

               
                return true;
            }
            catch (Exception ex)
            {
                Shutdown();
                error = $"open utun failed: {ex.Message}";
            }
            finally
            {
                Marshal.FreeHGlobal(arg);
            }
            return false;
        }

        public void Shutdown()
        {
            try
            {
                interfaceOsx = string.Empty;

                safeFileHandle?.Dispose();
                safeFileHandle = null;

                try { fsRead?.Flush(); } catch (Exception) { }
                try { fsRead?.Close(); fsRead?.Dispose(); } catch (Exception) { }
                fsRead = null;

                try { fsWrite?.Flush(); } catch (Exception) { }
                try { fsWrite?.Close(); fsWrite?.Dispose(); } catch (Exception) { }
                fsWrite = null;
            }
            catch (Exception)
            {
            }
            IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(this.prefixLength));
            CommandHelper.Osx(string.Empty, new string[] { $"route delete -net {network}/{prefixLength} {address}" });
        }
        public void Refresh()
        {
        }

        public void AddRoute(LinkerTunDeviceRouteItem[] ips)
        {
            string[] commands = ips.Select(item =>
            {
                IPAddress _ip = NetworkHelper.ToNetworkIP(item.Address, item.PrefixLength);
                return $"route add -net {_ip}/{item.PrefixLength} {address}";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands.ToArray());
            }
        }
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ips)
        {
            string[] commands = ips.Select(item =>
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
            CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {Name} mtu {value} up" });
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


        private byte[] buffer = new byte[65 * 1024];
        private readonly object writeLockObj = new object();
        public byte[] Read(out int length)
        {
            length = 0;
            if (safeFileHandle == null) return Helper.EmptyArray;

            length = fsRead.Read(buffer.AsSpan(4));
            length.ToBytes(buffer);
            length += 4;
            return buffer;
        }

        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            if (safeFileHandle == null) return true;
            lock (writeLockObj)
            {
                fsWrite.Write(buffer.Span);
                fsWrite.Flush();
                return true;
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

        public async Task<bool> CheckAvailable(bool order = false)
        {
            return await Task.FromResult(true).ConfigureAwait(false);
        }
    }
}
