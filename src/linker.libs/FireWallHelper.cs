using System;
using System.IO;
using System.Net;

namespace linker.libs
{
    public static class FireWallHelper
    {
        public static void Write(string fileName, IPAddress ip, byte prefixLength)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName, ip, prefixLength);
            }
        }
        public static void Write(string fileName)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName);
            }
        }
        private static void Windows(string fileName)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                CommandHelper.Windows(string.Empty, new string[] {
                    $"netsh advfirewall firewall delete rule name=\"{name}-any\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-any\" dir=in action=allow program=\"{fileName}\" enable=yes"
                });
            }
            catch (Exception)
            {
            }
        }
        private static void Windows(string fileName, IPAddress ip, byte prefixLength)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                CommandHelper.Windows(string.Empty, new string[] {
                    $"netsh advfirewall firewall delete rule name=\"{name}-any\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-any\" dir=in action=allow program=\"{fileName}\" enable=yes",
                    $"netsh advfirewall firewall delete rule name=\"{name}-icmp\"",
                    $"netsh advfirewall firewall add rule name=\"{name}-icmp\" dir=in action=allow protocol=icmpv4 localip={ip}/{prefixLength} enable=yes",
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
