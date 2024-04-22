using common.libs;
using System.Security.Principal;

namespace cmonitor.plugins.ntrights
{
    public static class NtrightsHelper
    {
        public static void AddTokenPrivilege()
        {
			try
			{
                if (OperatingSystem.IsWindows())
                {
                    WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                    CommandHelper.Windows(string.Empty, new string[] {
                    $"plugins/ntrights/ntrights.exe +r SeAssignPrimaryTokenPrivilege -u {windowsIdentity.Name}"
                });
                }
            }
			catch (Exception)
			{
			}
        }
    }
}
