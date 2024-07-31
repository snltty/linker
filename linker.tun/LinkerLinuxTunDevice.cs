using linker.libs;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace linker.tun
{
    public sealed class LinkerLinuxTunDevice : ILinkerTunDevice
    {

        private string name = string.Empty;
        public string Name => name;
        public bool Running => fs != null;

        private string interfaceLinux = string.Empty;
        private FileStream fs = null;

        public LinkerLinuxTunDevice(string name)
        {
            this.name = name;
        }

        public bool SetUp(IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
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
            int length = fs.Read(buffer, 0, buffer.Length);
            return buffer.AsMemory(0, length);
        }
        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            fs.Write(buffer.Span);
            return true;
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
