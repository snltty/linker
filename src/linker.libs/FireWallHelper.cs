using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace linker.libs
{
    public static class FireWallHelper
    {
        public static void Write(string fileName)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName);
            }
        }
        public static void Write(string fileName, IPAddress ip, byte prefixLength, (IPAddress ip, IPAddress mapIp, byte prefixLength)[] lans)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName, ip, prefixLength, lans);
            }
        }
        private static void Windows(string fileName)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                CommandHelper.Windows(string.Empty, new string[] {
                    $"netsh advfirewall firewall delete rule name=\"{name}\"",
                    $"netsh advfirewall firewall delete rule name=\"{name}-any\"",
                    $"netsh advfirewall firewall delete rule name=\"{name}-tcp\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-tcp\" dir=in action=allow protocol=tcp program=\"{fileName}\" enable=yes",
                    $"netsh advfirewall firewall delete rule name=\"{name}-udp\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-udp\" dir=in action=allow protocol=udp program=\"{fileName}\" enable=yes"
                });
            }
            catch (Exception)
            {
            }
        }
        private static void Windows(string fileName, IPAddress ip, byte prefixLength, (IPAddress ip, IPAddress mapIp, byte prefixLength)[] lans)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                CommandHelper.Windows(string.Empty, new string[] {
                    $"netsh advfirewall firewall delete rule name=\"{name}\"",
                    $"netsh advfirewall firewall delete rule name=\"{name}-any\"",
                    $"netsh advfirewall firewall delete rule name=\"{name}-tcp\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-tcp\" dir=in action=allow protocol=tcp program=\"{fileName}\" enable=yes",
                    $"netsh advfirewall firewall delete rule name=\"{name}-udp\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-udp\" dir=in action=allow protocol=udp program=\"{fileName}\" enable=yes",
                    $"netsh advfirewall firewall delete rule name=\"{name}-icmp\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-icmp\" dir=in action=allow protocol=icmpv4 localip={ip}/{prefixLength} enable=yes",
                });
                string[] add = lans.Select(c => $"netsh advfirewall firewall add rule name=\"{name}-icmp\" dir=in action=allow protocol=icmpv4 localip={(IPAddress.Any.Equals(c.mapIp) ? c.ip : c.mapIp)}/{c.prefixLength} enable=yes").ToArray();
                CommandHelper.Windows(string.Empty, add);
            }
            catch (Exception)
            {
            }
        }
    }
}
