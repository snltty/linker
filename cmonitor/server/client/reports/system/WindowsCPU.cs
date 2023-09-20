using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.system
{

    public static class CPUHelper
    {
        public static CPUTime GetCPUTime()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return WindowsCPU.GetCPUTime();
            return new CPUTime();
        }

        public static double CalculateCPULoad(CPUTime oldTime, CPUTime newTime)
        {
            ulong totalTicksSinceLastTime = newTime.SystemTime - oldTime.SystemTime;
            ulong idleTicksSinceLastTime = newTime.IdleTime - oldTime.IdleTime;

            double ret = 1.0f - ((totalTicksSinceLastTime > 0) ? ((double)idleTicksSinceLastTime) / totalTicksSinceLastTime : 0);

            return Math.Round(ret * 100, 2);
        }
    }


    public partial class WindowsCPU
    {
#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);
#else
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);
#endif
        public static CPUTime GetCPUTime(FILETIME lpIdleTime, FILETIME lpKernelTime, FILETIME lpUserTime)
        {
            var IdleTime = ((ulong)lpIdleTime.DateTimeHigh << 32) | lpIdleTime.DateTimeLow;
            var KernelTime = ((ulong)lpKernelTime.DateTimeHigh << 32) | lpKernelTime.DateTimeLow;
            var UserTime = ((ulong)lpUserTime.DateTimeHigh << 32) | lpUserTime.DateTimeLow;

            var SystemTime = KernelTime + UserTime;

            return new CPUTime(IdleTime, SystemTime);
        }

        public static CPUTime GetCPUTime()
        {
            FILETIME lpIdleTime = default;
            FILETIME lpKernelTime = default;
            FILETIME lpUserTime = default;
            if (!GetSystemTimes(out lpIdleTime, out lpKernelTime, out lpUserTime))
            {
                return default;
            }
            return GetCPUTime(lpIdleTime, lpKernelTime, lpUserTime);
        }
    }

    public struct CPUTime
    {
        public CPUTime(ulong idleTime, ulong systemTime)
        {
            IdleTime = idleTime;
            SystemTime = systemTime;
        }

        public ulong IdleTime { get; private set; }
        public ulong SystemTime { get; private set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint DateTimeLow;
        public uint DateTimeHigh;
    }
}
