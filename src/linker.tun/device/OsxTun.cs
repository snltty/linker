using System.Runtime.InteropServices;

namespace linker.tun.device
{
    internal static class OsxAPI
    {
        // Required P/Invoke for macOS UTUN API
        [DllImport("libutunshim.dylib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int open_utun(int unit, IntPtr ifnameBuf, UIntPtr ifnameLen, out int out_errno);
    }
}
