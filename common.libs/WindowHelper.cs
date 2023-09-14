using System;
using System.Diagnostics;
using System.Linq;

namespace common.libs
{
    public class WindowHelper
    {
        #region 进程
        public static Process[] processes;
        public static void UpdateCurrentProcesses()
        {
            processes = Process.GetProcesses();
        }
        public static bool GetHasWindowByName(string name)
        {
            return processes != null && processes.Any(c => c.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

    }
}
