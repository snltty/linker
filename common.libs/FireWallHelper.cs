using System;
using System.IO;

namespace common.libs
{
    public static class FireWallHelper
    {
        public static void Write(string fileName, string distPatt)
        {
            if (OperatingSystem.IsWindows())
            {
                Windows(fileName, distPatt);
            }
            else if (OperatingSystem.IsLinux())
            {
                Linux(fileName);
            }
        }

        private static void Linux(string fileName)
        {
            CommandHelper.Linux(string.Empty,new string[] {
                $"firewall-cmd --permanent --new-service={fileName}",
                $"firewall-cmd --permanent --service={fileName} --set-short=\"My Application {fileName}\"",
                $"firewall-cmd --permanent --service={fileName} --set-description=\"Allow all ports for My Application {fileName}\"",
                $"firewall-cmd --permanent --service={fileName} --add-port=0-65535/tcp",
                $"firewall-cmd --permanent --service={fileName} --add-port=0-65535/udp",
                $"firewall-cmd --permanent --add-service={fileName}",
                $"firewall-cmd --reload",
            });
        }

        private static void Windows(string fileName,string distPatth)
        {
            try
            {
                string content = $@"@echo off
cd  ""%CD%""
for /f ""tokens=4,5 delims=. "" %%a in ('ver') do if %%a%%b geq 60 goto new

:old
cmd /c netsh firewall delete allowedprogram program=""%CD%\{fileName}.exe"" profile=ALL
cmd /c netsh firewall add allowedprogram program=""%CD%\{fileName}.exe"" name=""{fileName}"" ENABLE
cmd /c netsh firewall add allowedprogram program=""%CD%\{fileName}.exe"" name=""{fileName}"" ENABLE profile=ALL
goto end
:new
cmd /c netsh advfirewall firewall delete rule name=""{fileName}""
cmd /c netsh advfirewall firewall add rule name=""{fileName}"" dir=in action=allow program=""%CD%\{fileName}.exe"" protocol=tcp enable=yes profile=public
cmd /c netsh advfirewall firewall add rule name=""{fileName}"" dir=in action=allow program=""%CD%\{fileName}.exe"" protocol=udp enable=yes profile=public
cmd /c netsh advfirewall firewall add rule name=""{fileName}"" dir=in action=allow program=""%CD%\{fileName}.exe"" protocol=tcp enable=yes profile=domain
cmd /c netsh advfirewall firewall add rule name=""{fileName}"" dir=in action=allow program=""%CD%\{fileName}.exe"" protocol=udp enable=yes profile=domain
cmd /c netsh advfirewall firewall add rule name=""{fileName}"" dir=in action=allow program=""%CD%\{fileName}.exe"" protocol=tcp enable=yes profile=private
cmd /c netsh advfirewall firewall add rule name=""{fileName}"" dir=in action=allow program=""%CD%\{fileName}.exe"" protocol=udp enable=yes profile=private
:end";
                if(Directory.Exists(distPatth) == false)
                {
                    Directory.CreateDirectory(distPatth);
                }
                string firewall = Path.Join(distPatth, "firewall.bat");

                System.IO.File.WriteAllText(firewall, content);
                CommandHelper.Execute(firewall, string.Empty, new string[0]);
                System.IO.File.Delete(firewall);
            }
            catch (Exception)
            {
            }
        }
    }
}
