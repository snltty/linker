using linker.libs.extends;
using System;
namespace linker.libs
{
    public static class SystemIdHelper
    {
        public static string GetSystemId()
        {
            return OperatingSystem.IsWindows() ? GetSystemIdWindows() : OperatingSystem.IsLinux() ? GetSystemIdLinux() : GetSystemIdOSX();
        }

        private static string GetSystemIdWindows()
        {
            string cpu = CommandHelper.Execute("wmic", "csproduct get UUID", [], out string error).TrimNewLineAndWhiteSapce().Split(Environment.NewLine)[1];
            string username = CommandHelper.Execute("whoami", string.Empty, [], out error).TrimNewLineAndWhiteSapce().Trim();
            return $"{cpu}↓{username}↓{System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
        }
        private static string GetSystemIdLinux()
        {
            string cpu = CommandHelper.Linux(string.Empty, ["cat /etc/machine-id"]).TrimNewLineAndWhiteSapce();
            if (string.IsNullOrWhiteSpace(cpu) || cpu.Contains("No such file or directory"))
            {
                cpu = string.Empty;
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
