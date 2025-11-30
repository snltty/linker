using linker.libs.extends;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
            string localAppDataPath = Path.Join(Helper.CurrentDirectory, "machine-id.txt");
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

        public static string GetSystemStr()
        {
            return $"{SystemName()}-{VersionNumber()}-{RuntimeInformation.OSArchitecture.ToString().ToLower()}";
        }
        private static string SystemName()
        {
            string pattern = @"ikuai|fnos|iphone|samsung|vivo|oppo|google|huawei|xiaomi|ios|android|windows|ubuntu|openwrt|armbian|archlinux|fedora|centos|rocky|alpine|debian|linux|docker";
            return Regex.Match(RuntimeInformation.OSDescription.ToLower(), pattern)?.Value ?? "unknow";
        }
        private static string VersionNumber()
        {
            var version = Environment.OSVersion.Version;
            if (OperatingSystem.IsWindows())
            {
                return version.Major switch
                {
                    10 when version.Build >= 22000 => "11",
                    10 when version.Build >= 10240 => "10",
                    6 when version.Minor == 3 => "8.1",
                    6 when version.Minor == 2 => "8",
                    6 when version.Minor == 1 => "7",
                    6 when version.Minor == 0 => "Vista",
                    5 when version.Minor == 2 => "2003",
                    5 when version.Minor == 1 => "XP",
                    5 when version.Minor == 0 => "2000",
                    _ => $"unknow"
                };
            }
            if (OperatingSystem.IsAndroid())
            {
                return version.Major switch
                {
                    35 => "15",
                    34 => "14",
                    33 => "13",
                    32 => "12L",
                    31 => "12",
                    30 => "11",
                    29 => "10",
                    28 => "9",
                    27 => "8.1",
                    26 => "8.0",
                    25 => "7.1",
                    24 => "7.0",
                    23 => "6.0",
                    22 => "5.1",
                    21 => "5.0",
                    _ => $"unknow"
                };
            }
            if (OperatingSystem.IsLinux())
            {
                return $"{version.Major}.x";
            }

            return version.ToString();
        }
    }
}
