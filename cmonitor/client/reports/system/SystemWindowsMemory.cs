using System.Runtime.InteropServices;

namespace cmonitor.client.reports.system
{
    public static class SystemWindowsMemory
    {

        public static double GetMemoryUsage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MEMORYSTATUSEX memoryStatus = new MEMORYSTATUSEX();
                memoryStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

                if (GlobalMemoryStatusEx(ref memoryStatus) == false)
                {
                    return 0;
                }

                return Math.Round((float)(memoryStatus.ullTotalPhys - memoryStatus.ullAvailPhys) / memoryStatus.ullTotalPhys * 100f, 2);
            }
            return 0;
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }
    }
}
