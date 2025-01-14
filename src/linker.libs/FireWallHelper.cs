using System;
using System.IO;

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
            else if (OperatingSystem.IsLinux())
            {
                Linux(fileName);
            }
        }

        private static void Linux(string fileName)
        {
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
        }

        private static void Windows(string fileName)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                CommandHelper.Windows(string.Empty, new string[] {
                    $"netsh advfirewall firewall delete rule name=\"{name}\"",
                    $"netsh advfirewall firewall add rule name=\"{name}\" dir=in action=allow program=\"{fileName}\" protocol=tcp enable=yes",
                    $"netsh advfirewall firewall add rule name=\"{name}\" dir=in action=allow program=\"{fileName}\" protocol=udp enable=yes",
                    $"netsh advfirewall firewall add rule name=\"{name}\" dir=in action=allow program=\"{fileName}\" protocol=icmpv4 enable=yes",
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
