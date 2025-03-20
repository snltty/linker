using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace linker.libs
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class LinuxAPI
    {
        public const int EINTR = 4;
        public const int EAGAIN = 11;

        [DllImport("libc", SetLastError = true)]
        public static extern int poll([In, Out] PollFD[] fds, int nfds, int timeout);
    }

    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public struct PollFD
    {
        public int fd;
        public short events;
        public short revents;
    }

    public enum PollEvent : short
    {
        In = 0x001,
        Out = 0x004
    }
}
