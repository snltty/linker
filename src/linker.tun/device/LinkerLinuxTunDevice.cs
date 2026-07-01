using linker.libs;
using linker.libs.extends;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace linker.tun.device
{
    internal sealed class LinkerLinuxTunDevice : ILinkerTunDevice
    {

        private string name = string.Empty;
        public string Name => name;
        public bool Running => safeFileHandle != null;

        private string interfaceLinux = string.Empty;
        private FileStream fsRead = null;
        private FileStream fsWrite = null;
        private SafeFileHandle safeFileHandle;
        private IPAddress address;
        private byte prefixLength = 24;

        public LinkerLinuxTunDevice()
        {
        }

        public bool Setup(LinkerTunDeviceSetupInfo info, out string error)
        {
            error = string.Empty;

            name = info.Name;
            address = info.Address;
            prefixLength = info.PrefixLength;

            if (Running)
            {
                error = $"Adapter already exists";
                return false;
            }
            if (Create(out error) == false)
            {
                return false;
            }
            if (Open(out error) == false)
            {
                Shutdown();
                return false;
            }
            fsRead = new FileStream(safeFileHandle, FileAccess.Read, 65 * 1024, true);
            fsWrite = new FileStream(safeFileHandle, FileAccess.Write, 65 * 1024, true);

            interfaceLinux = GetLinuxInterfaceNum();
            return true;
        }
        private bool Create(out string error)
        {
            error = string.Empty;

            //byte[] ipv6 = IPAddress.Parse("fe80::1818:1818:1818:1818").GetAddressBytes();
            //address.GetAddressBytes().CopyTo(ipv6, ipv6.Length - 4);

            CommandHelper.Linux(string.Empty, new string[] {
                $"ip tuntap add mode tun dev {Name}",
                $"ip addr add {address}/{prefixLength} dev {Name}",
                //$"ip addr add {new IPAddress(ipv6)}/64 dev {Name}",
                $"ip link set dev {Name} up"
            });

            string str = CommandHelper.Linux(string.Empty, new string[] { $"ifconfig" });
            if (str.Contains(Name) == false)
            {
                CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {Name}" }, out error);
                return false;
            }

            return true;
        }
        private bool Open(out string error)
        {
            error = string.Empty;

            SafeFileHandle _safeFileHandle = File.OpenHandle("/dev/net/tun", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.Asynchronous);
            if (_safeFileHandle.IsInvalid)
            {
                _safeFileHandle?.Dispose();
                Shutdown();
                error = $"open file /dev/net/tun fail {Marshal.GetLastWin32Error()}";
                return false;
            }

            int ioctl = LinuxAPI.Ioctl(Name, _safeFileHandle, 1074025674);
            if (ioctl != 0)
            {
                _safeFileHandle?.Dispose();
                Shutdown();
                error = $"Ioctl fail : {ioctl}，{Marshal.GetLastWin32Error()}";
                return false;
            }
            safeFileHandle = _safeFileHandle;

            return true;
        }

        public void Shutdown()
        {
            try
            {
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

            interfaceLinux = string.Empty;
            CommandHelper.Linux(string.Empty, new string[] { $"ip link del {Name}", $"ip tuntap del mode tun dev {Name}" });

            GC.Collect();
        }

        public void Refresh()
        {
            if (safeFileHandle == null) return;
            try
            {
                CommandHelper.Linux(string.Empty, new string[] {
                    $"ip link set dev {Name} up"
                });
            }
            catch (Exception)
            {
            }
        }

        public void SetMssFix(int value = 0)
        {
            CommandHelper.Linux(string.Empty, new string[] {
                @$"iptables-save | grep -v -E -- ""-[oi] {Name}\s*.*\s* -j TCPMSS"" | iptables-restore",
            });

            if (value >= 7 && value < 1500)
            {
                string _value = value == 7 ? "--clamp-mss-to-pmtu" : $"--set-mss {value}";

                string[] commands = new string[] {
                    $"iptables -t mangle -A INPUT -i {Name} -p tcp --syn -j TCPMSS {_value}",
                    $"iptables -t mangle -A INPUT -i {Name} -p tcp --tcp-flags SYN SYN -j TCPMSS {_value}",
                    $"iptables -t mangle -A OUTPUT -o {Name} -p tcp --syn -j TCPMSS {_value}",
                    $"iptables -t mangle -A OUTPUT -o {Name} -p tcp --tcp-flags SYN SYN -j TCPMSS {_value}",
                    $"iptables -t mangle -A FORWARD -i {Name} -o {interfaceLinux} -p tcp --syn -j TCPMSS {_value}",
                    $"iptables -t mangle -A FORWARD -i {Name} -o {interfaceLinux} -p tcp --tcp-flags SYN SYN -j TCPMSS {_value}",
                    $"iptables -t mangle -A FORWARD -i {interfaceLinux} -o {Name} -p tcp --syn -j TCPMSS {_value}",
                    $"iptables -t mangle -A FORWARD -i {interfaceLinux} -o {Name} -p tcp --tcp-flags SYN SYN -j TCPMSS {_value}",
                };
                string str = CommandHelper.Linux(string.Empty, commands);
            }
        }
        public void SetMtu(int value)
        {
            if (value > 0)
            {
                CommandHelper.Linux(string.Empty, new string[] { $"ip link set dev {Name} mtu {value}" });
            }
            else
            {
                CommandHelper.Linux(string.Empty, new string[] { $"ip link set dev {Name} mtu 1420" });
            }

        }

        private string GetDefaultInterface()
        {
            return CommandHelper.Linux(string.Empty, ["ip route show default | awk '{print $5}'"]);
        }
        public void SetNat(out string error)
        {
            error = string.Empty;
            if (address == null || address.Equals(IPAddress.Any))
            {
                return;
            }
            try
            {
                IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));

                string[] commands = new string[] {
                    $"sysctl -w net.ipv4.ip_forward=1",
                    $"sysctl -w net.ipv4.conf.{Name}.forwarding=1",
                    @$"iptables-save | grep -v -E -- ""-[oi] {Name}\s*.*\s* -j (ACCEPT|MASQUERADE|DROP|REJECT)"" | iptables-restore",

                    $"iptables -I FORWARD -i {Name} -j ACCEPT",
                    $"iptables -I FORWARD -o {Name} -j ACCEPT",

                    $"iptables -t nat -I POSTROUTING -o {Name} -j MASQUERADE",
                    $"iptables -t nat -I POSTROUTING ! -o {Name} -s {network}/{prefixLength} -j MASQUERADE",
                };
                string str = CommandHelper.Linux(string.Empty, commands);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        public void RemoveNat(out string error)
        {
            error = string.Empty;
            if (address == null || address.Equals(IPAddress.Any)) return;
            try
            {
                string[] commands = new string[] {
                    @$"iptables-save | grep -v -E -- ""-[oi] {Name}\s*.*\s* -j (ACCEPT|MASQUERADE|DROP|REJECT)"" | iptables-restore",
                    @$"iptables-save | grep -v -E -- ""-[oi] {Name}\s*.*\s* -j TCPMSS"" | iptables-restore",
                };
                string str = CommandHelper.Linux(string.Empty, commands);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }
        private void RestartFirewall()
        {
            if (RuntimeInformation.OSDescription.Contains("OpenWrt"))
            {
                CommandHelper.Linux(string.Empty, new string[] { "/etc/init.d/firewall restart" });
            }
        }

        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            string str = CommandHelper.Linux(string.Empty, new string[] { $"iptables -t nat -L PREROUTING" });
            IEnumerable<LinkerTunDeviceForwardItem> lines = str.Split(Environment.NewLine)
                .Select(c => Regex.Replace(c, @"\s+", " ").Split(' '))
                .Where(c => c.Length > 0 && c[0] == "DNAT" && c[1] == "tcp")
                .Select(c =>
                {
                    IPEndPoint dist = IPEndPoint.Parse(c[^1].Replace("to:", ""));
                    int port = int.Parse(c[^2].Replace("dpt:", ""));
                    return new LinkerTunDeviceForwardItem { ListenAddr = IPAddress.Any, ListenPort = port, ConnectAddr = dist.Address, ConnectPort = dist.Port };
                });
            return lines.ToList();
        }
        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            string[] commands = forwards.Where(c => c != null && c.Enable).SelectMany(c =>
            {
                return new string[] {
                    $"sysctl -w net.ipv4.ip_forward=1",
                    $"iptables -t nat -A PREROUTING -p tcp --dport {c.ListenPort} -j DNAT --to-destination {c.ConnectAddr}:{c.ConnectPort}",
                    $"iptables -t nat -A POSTROUTING -p tcp --dport {c.ConnectPort} -j MASQUERADE",
                    $"iptables -t nat -A PREROUTING -p udp --dport {c.ListenPort} -j DNAT --to-destination {c.ConnectAddr}:{c.ConnectPort}",
                    $"iptables -t nat -A POSTROUTING -p udp --dport {c.ConnectPort} -j MASQUERADE",
                };

            }).ToArray();
            if (commands.Length > 0)
                CommandHelper.Linux(string.Empty, commands);
        }
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            string[] commands = forwards.Where(c => c != null && c.Enable).SelectMany(c =>
            {
                return new string[] {
                    $"sysctl -w net.ipv4.ip_forward=1",
                    $"iptables -t nat -D PREROUTING -p tcp --dport {c.ListenPort} -j DNAT --to-destination {c.ConnectAddr}:{c.ConnectPort}",
                    $"iptables -t nat -D POSTROUTING -p tcp --dport {c.ConnectPort} -j MASQUERADE",
                    $"iptables -t nat -D PREROUTING -p udp --dport {c.ListenPort} -j DNAT --to-destination {c.ConnectAddr}:{c.ConnectPort}",
                    $"iptables -t nat -D POSTROUTING -p udp --dport {c.ConnectPort} -j MASQUERADE"
                };

            }).ToArray();
            if (commands.Length > 0)
                CommandHelper.Linux(string.Empty, commands);
        }


        public void AddRoute(LinkerTunDeviceRouteItem[] ips)
        {
            string[] commands = ips.Select(item =>
            {
                uint prefixValue = NetworkHelper.ToPrefixValue(item.PrefixLength);
                IPAddress network = NetworkHelper.ToNetworkIP(item.Address, prefixValue);

                return $"ip route add {network}/{item.PrefixLength} via {address} dev {Name} metric 1 ";
            }).ToArray();
            if (commands.Length > 0)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning($"tuntap linux add route: {string.Join("\r\n", commands)}");
                CommandHelper.Linux(string.Empty, commands);
            }
        }
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                uint prefixValue = NetworkHelper.ToPrefixValue(item.PrefixLength);
                IPAddress network = NetworkHelper.ToNetworkIP(item.Address, prefixValue);
                return $"ip route del {network}/{item.PrefixLength}";
            }).ToArray();

            if (commands.Length > 0)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning($"tuntap linux del route: {string.Join("\r\n", commands)}");
                CommandHelper.Linux(string.Empty, commands);
            }

        }


        private readonly byte[] buffer = new byte[128 * 1024];
        private readonly object writeLockObj = new object();
        public byte[] Read(out uint length)
        {
            length = 0;
            if (safeFileHandle == null)
            {
                return Helper.EmptyArray;
            }

            length = (uint)fsRead.Read(buffer.AsSpan(4));
            ((ushort)(length + 2)).ToBytes(buffer.AsSpan());
            buffer[2] = 0;
            buffer[3] = 0;
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

        private string GetLinuxInterfaceNum()
        {
            string output = CommandHelper.Linux(string.Empty, new string[] { "ip route show default | awk '{print $5}'" }).TrimNewLineAndWhiteSapce();
            return output;
        }

        public  Task<bool> CheckAvailable(bool order = false)
        {
            string output = CommandHelper.Linux(string.Empty, new string[] { $"ip link show {Name}" });
            return  Task.FromResult(output.Contains("state UP"));
        }
    }
}
