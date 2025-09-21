using System.Runtime.InteropServices;
using System.Text;

namespace linker.tun.device
{
    internal static class LinuxAPI
    {

        internal const int O_ACCMODE = 0x00000003;
        internal const int O_RDONLY = 0x00000000;
        internal const int O_WRONLY = 0x00000001;
        internal const int O_RDWR = 0x00000002;
        internal const int O_CREAT = 0x00000040;
        internal const int O_EXCL = 0x00000080;
        internal const int O_NOCTTY = 0x00000100;
        internal const int O_TRUNC = 0x00000200;
        internal const int O_APPEND = 0x00000400;
        internal const int O_NONBLOCK = 0x00000800;
        internal const int O_NDELAY = 0x00000800;
        internal const int O_SYNC = 0x00101000;
        internal const int O_ASYNC = 0x00002000;

        [DllImport("libc.so.6", EntryPoint = "open", SetLastError = true)]
        internal static extern int Open(string fileName, int mode);
        [DllImport("libc.so.6", EntryPoint = "close", SetLastError = true)]
        internal static extern int Close(int fd);

        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        internal static extern int Ioctl(int fd, uint request, byte[] dat);
        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        internal static extern int Ioctl(SafeHandle device, uint request, byte[] dat);

        [DllImport("libc.so.6", EntryPoint = "read", SetLastError = true)]
        internal static extern int Read(int handle, byte[] data, int length);
        [DllImport("libc.so.6", EntryPoint = "read", SetLastError = true)]
        internal static extern int Read(int handle, nint data, int length);

        [DllImport("libc.so.6", EntryPoint = "write", SetLastError = true)]
        internal static extern int Write(int handle, byte[] data, int length);


        internal static int Ioctl(string name, SafeHandle device, uint request)
        {
            byte[] ifreqFREG0 = Encoding.ASCII.GetBytes(name);
            Array.Resize(ref ifreqFREG0, 16);
            byte[] ifreqFREG1 = { 0x01, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] ifreq = BytesPlusBytes(ifreqFREG0, ifreqFREG1);
            return Ioctl(device, request, ifreq);
        }
        internal static byte[] BytesPlusBytes(byte[] A, byte[] B)
        {
            byte[] ret = new byte[A.Length + B.Length - 1 + 1];
            int k = 0;
            for (var i = 0; i <= A.Length - 1; i++)
                ret[i] = A[i];
            k = A.Length;
            for (var i = k; i <= ret.Length - 1; i++)
                ret[i] = B[i - k];
            return ret;
        }
    }
}
