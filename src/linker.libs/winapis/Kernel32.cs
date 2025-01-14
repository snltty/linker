using System;
using System.Runtime.InteropServices;

namespace linker.libs.winapis;

public static unsafe class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(nint hSnapshot);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern nint GetCommandLine();

    [DllImport("kernel32.dll")]
    public static extern nint GetConsoleWindow();

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")]
    public static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll")]
    public static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

    [DllImport("kernel32.dll")]
    public static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        /// <summary>
        /// Size of the structure, in bytes. You must set this member before calling GlobalMemoryStatusEx.
        /// </summary>
        public uint dwLength;

        /// <summary>
        /// Number between 0 and 100 that specifies the approximate percentage of physical memory that is in use (0 indicates no memory use and 100 indicates full memory use).
        /// </summary>
        public uint dwMemoryLoad;

        /// <summary>
        /// Total size of physical memory, in bytes.
        /// </summary>
        public ulong ullTotalPhys;

        /// <summary>
        /// Size of physical memory available, in bytes.
        /// </summary>
        public ulong ullAvailPhys;

        /// <summary>
        /// Size of the committed memory limit, in bytes. This is physical memory plus the size of the page file, minus a small overhead.
        /// </summary>
        public ulong ullTotalPageFile;

        /// <summary>
        /// Size of available memory to commit, in bytes. The limit is ullTotalPageFile.
        /// </summary>
        public ulong ullAvailPageFile;

        /// <summary>
        /// Total size of the user mode portion of the virtual address space of the calling process, in bytes.
        /// </summary>
        public ulong ullTotalVirtual;

        /// <summary>
        /// Size of unreserved and uncommitted memory in the user mode portion of the virtual address space of the calling process, in bytes.
        /// </summary>
        public ulong ullAvailVirtual;

        /// <summary>
        /// Size of unreserved and uncommitted memory in the extended portion of the virtual address space of the calling process, in bytes.
        /// </summary>
        public ulong ullAvailExtendedVirtual;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MEMORYSTATUSEX"/> class.
        /// </summary>
        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll")]
    public static extern bool SetHandleInformation(IntPtr hObject, int dwMask, int dwFlags);


    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern nint CreateFileA(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
           nint lpSecurityAttributes,
           uint dwCreationDisposition,
           uint dwFlagsAndAttributes,
           nint hTemplateFile);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool DeviceIoControl(nint hDevice, uint dwIoControlCode, nint lpInBuffer, uint nInBufferSize, nint lpOutBuffer, uint nOutBufferSize, ulong* lpBytesReturned, uint lpOverlapped);


    public struct SYSTEMTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetSystemTime(ref SYSTEMTIME time);

    public static bool Kill(uint pid)
    {
        IntPtr hProcess = OpenProcess(0x0001, false, pid);
        if (hProcess == IntPtr.Zero)
        {
            return false;
        }
        bool res = TerminateProcess(hProcess, 0);

        CloseHandle(hProcess);
        return res;
    }
}
