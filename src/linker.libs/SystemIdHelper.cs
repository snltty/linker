using linker.libs.extends;
using System;
using System.IO;
namespace linker.libs
{
    public static class SystemIdHelper
    {
        public static string GetSystemId()
        {
            if (OperatingSystem.IsWindows()) return GetSystemIdWindows();
            if (OperatingSystem.IsLinux()) return GetSystemIdLinux();
            if (OperatingSystem.IsAndroid()) return GetSystemIdAndroid();
            if (OperatingSystem.IsMacOS()) return GetSystemIdOSX();

            return string.Empty;
        }

        private static string GetSystemIdAndroid()
        {
            string localAppDataPath = Path.Join(Helper.currentDirectory, "machine-id.txt");
            if (Directory.Exists(Path.GetDirectoryName(localAppDataPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localAppDataPath));
            }
            if (File.Exists(localAppDataPath) == false)
            {
                File.WriteAllText(localAppDataPath, Guid.NewGuid().ToString());
            }
            return $"{File.ReadAllText(localAppDataPath)}↓android↓{System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
        }
        private static string GetSystemIdWindows()
        {
            string localAppDataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Helper.GlobalString, "machine-id.txt");
            if (Directory.Exists(Path.GetDirectoryName(localAppDataPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localAppDataPath));
            }
            if (File.Exists(localAppDataPath) == false)
            {
                File.WriteAllText(localAppDataPath, Guid.NewGuid().ToString());
            }
            string username = CommandHelper.Execute("whoami", string.Empty, [], out string error).TrimNewLineAndWhiteSapce();
            return $"{File.ReadAllText(localAppDataPath)}↓{username}↓{System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
        }
        private static string GetSystemIdLinux()
        {
            string cpu = CommandHelper.Linux(string.Empty, ["cat /etc/machine-id"]).TrimNewLineAndWhiteSapce();
            if (string.IsNullOrWhiteSpace(cpu) || cpu.Contains("No such file or directory"))
            {
                cpu = Guid.NewGuid().ToString();
                CommandHelper.Linux(string.Empty, [$"echo \"{cpu}\" > /etc/machine-id"]);
            }

            string username = CommandHelper.Linux(string.Empty, ["whoami"]).TrimNewLineAndWhiteSapce();
            return $"{cpu}↓{username}↓{System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
        }
        private static string GetSystemIdOSX()
        {
            string cpu = CommandHelper.Osx(string.Empty, ["system_profiler SPHardwareDataType | grep \"Hardware UUID\""]).TrimNewLineAndWhiteSapce();
            string username = CommandHelper.Osx(string.Empty, ["whoami"]).TrimNewLineAndWhiteSapce();
            return $"{cpu}↓{username}↓{System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
        }
    }
}
