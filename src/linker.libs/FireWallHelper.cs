using System;
using System.IO;
using System.Net;

namespace linker.libs
{
    public static class FireWallHelper
    {
        public static void WriteAny(string fileName)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName);
            }
            else if (OperatingSystem.IsLinux())
            {
                Linux(fileName);
            }
        }
        public static void WriteIcmp(string fileName, IPAddress ip, byte prefixLength)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName, ip, prefixLength);
            }
        }

        private static void Linux(string fileName)
        {
            /*
            fileName = Path.GetFileNameWithoutExtension(fileName);
            CommandHelper.Linux(string.Empty, new string[] {
                $"firewall-cmd --permanent --new-service={fileName}",
                $"firewall-cmd --permanent --service={fileName} --set-short=\"My Application {fileName}\"",
                $"firewall-cmd --permanent --service={fileName} --set-description=\"Allow all ports for my application {fileName}\"",
                $"firewall-cmd --permanent --service={fileName} --add-port=0-65535/tcp",
                $"firewall-cmd --permanent --service={fileName} --add-port=0-65535/udp",
                $"firewall-cmd --permanent --service={fileName} --add-port=0-65535/icmp",
                $"firewall-cmd --permanent --add-service={fileName}",
                $"firewall-cmd --reload",
            });
            */
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
