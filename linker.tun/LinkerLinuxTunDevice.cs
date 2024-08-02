using linker.libs;
using linker.libs.extends;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace linker.tun
{
    internal sealed class LinkerLinuxTunDevice : ILinkerTunDevice
    {

        private string name = string.Empty;
        public string Name => name;
        public bool Running => fs != null;

        private string interfaceLinux = string.Empty;
        private FileStream fs = null;
        private string iptableLineNumber = string.Empty;
        private IPAddress address;
        private byte prefixLength = 24;

        public LinkerLinuxTunDevice(string name)
        {
            this.name = name;
        }

        public bool SetUp(IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
            this.address = address;
            this.prefixLength = prefixLength;
            error = string.Empty;
            if (fs != null)
            {
                error = ($"Adapter already exists");
                return false;
            }

            CommandHelper.Linux(string.Empty, new string[] {
                $"ip tuntap add mode tun dev {Name}",
                $"ip addr del {address}/{prefixLength} dev {Name}",
                $"ip addr add {address}/{prefixLength} dev {Name}",
                $"ip link set dev {Name} up"
            });

            string str = CommandHelper.Linux(string.Empty, new string[] { $"ifconfig" });
            if (str.Contains(Name) == false)
            {
                error = CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap add mode tun dev {Name}" });
                return false;
            }

            interfaceLinux = GetLinuxInterfaceNum();

            SafeFileHandle safeFileHandle = File.OpenHandle("/dev/net/tun", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.Asynchronous);

            byte[] ifreqFREG0 = Encoding.ASCII.GetBytes(this.Name);
            Array.Resize(ref ifreqFREG0, 16);
            byte[] ifreqFREG1 = { 0x01, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] ifreq = BytesPlusBytes(ifreqFREG0, ifreqFREG1);
            Ioctl(safeFileHandle, 1074025674, ifreq);
            fs = new FileStream(safeFileHandle, FileAccess.ReadWrite, 1500);

            return true;
        }
        public void Shutdown()
        {
            if (fs != null)
            {
                interfaceLinux = string.Empty;
                fs.Close();
                fs.Dispose();
                fs = null;
            }
            CommandHelper.Linux(string.Empty, new string[] { $"ip tuntap del mode tun dev {Name}" });
        }

        public void SetMtu(int value)
        {
            CommandHelper.Linux(string.Empty, new string[] { $"ip link set dev {Name} mtu {value}" });
        }
        public void SetNat()
        {
            IPAddress network = NetworkHelper.ToNetworkIp(address, NetworkHelper.MaskValue(prefixLength));
            CommandHelper.PowerShell(string.Empty, new string[] {
                $"sysctl -w net.ipv4.ip_forward=1",
                $"iptables -t nat -A POSTROUTING ! -o {Name} -s {network}/{prefixLength} -j MASQUERADE",
            });
            iptableLineNumber = CommandHelper.Linux(string.Empty, new string[] { $"iptables -t nat -L --line-numbers | grep {network}/{prefixLength} | cut -d' ' -f1" });
        }
        public void RemoveNat()
        {
            if (string.IsNullOrWhiteSpace(iptableLineNumber) == false)
            {
                CommandHelper.PowerShell(string.Empty, new string[] { $"iptables -t nat -D POSTROUTING {iptableLineNumber}" });
            }
        }


        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            string[] commands = ips.Select(item =>
            {
                uint maskValue = NetworkHelper.MaskValue(item.Mask);
                IPAddress _ip = NetworkHelper.ToNetworkIp(item.Address, maskValue);

                return $"ip route add {_ip}/{item.Mask} via {ip} dev {Name} metric 1 ";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Linux(string.Empty, commands);
            }
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                uint maskValue = NetworkHelper.MaskValue(item.Mask);
                IPAddress _ip = NetworkHelper.ToNetworkIp(item.Address, maskValue);
                return $"ip route del {_ip}/{item.Mask}";
            }).ToArray();
            CommandHelper.Linux(string.Empty, commands);
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


        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int Ioctl(SafeHandle device, UInt32 request, byte[] dat);
        private byte[] BytesPlusBytes(byte[] A, byte[] B)
        {
            byte[] ret = new byte[A.Length + B.Length - 1 + 1];
            int k = 0;
            for (var i = 0; i <= A.Length - 1; i++)
                ret[i] = A[i];
            k = A.Length;
            for (var i = k; i <= ret.Length - 1; i++)
                ret[i] = B[i - k];
            return ret;
        }


    }
}
