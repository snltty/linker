using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace linker.tun
{
    internal static class OsxAPI
    {

        // 定义 macOS 的 ioctl 命令（来自 <net/if_utun.h>）
        private const uint UTUN_CONTROL = 0x80000000; // 'u' << 24
        private const uint UTUN_CTRL_SET_IFNAME = (UTUN_CONTROL | 1);

        // P/Invoke 声明
        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int Ioctl(SafeFileHandle fd, uint request, IntPtr arg);

        public static int Ioctl(SafeFileHandle fd, IntPtr arg)
        {
            return Ioctl(fd, UTUN_CTRL_SET_IFNAME, arg);
        }
    }
}
