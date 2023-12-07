using System;
using System.Runtime.InteropServices;

namespace cmonitor.libs
{
    internal sealed class ShareMemoryLinux : IShareMemory
    {
        private string key;
        private int length;
        private int itemSize;

        long shmSize;
        IntPtr shmPtr;

        public ShareMemoryLinux(string key, int length, int itemSize)
        {
            this.key = key;
            this.length = length;
            this.itemSize = itemSize;
        }

        public bool Init()
        {
            try
            {
                int shmFd = ShmOpen(key, 0, 0666);
                if (shmFd == -1)
                {
                    return false;
                }

                shmSize = length * itemSize;
                int result = FTruncate(shmFd, shmSize);
                if (result == -1)
                {
                    return false;
                }

                shmPtr = MMap(IntPtr.Zero, (IntPtr)shmSize, 0x03, 0x1 /* MAP_SHARED */, shmFd, 0);
                return shmPtr != IntPtr.Zero;
            }
            catch (Exception)
            {

            }
            return false;
        }

        public void ReadArray(int position, byte[] bytes, int offset, int length)
        {
            if (shmPtr != IntPtr.Zero)
            {
                Marshal.Copy(shmPtr + position, bytes, offset, length);
            }
        }
        public void WriteArray(int position, byte[] bytes, int offset, int length)
        {
            if (shmPtr != IntPtr.Zero)
            {
                Marshal.Copy(bytes, offset, shmPtr + position, length);
            }
        }

        public byte ReadByte(int position)
        {
            if (shmPtr != IntPtr.Zero)
            {
                return Marshal.ReadByte(shmPtr + position);
            }
            return 0;
        }
        public void WriteByte(int position, byte value)
        {
            if (shmPtr != IntPtr.Zero)
            {
                Marshal.WriteByte(shmPtr + position, value);
            }
        }

        public int ReadInt(int position)
        {
            if (shmPtr != IntPtr.Zero)
            {
                return Marshal.ReadInt32(shmPtr + position);
            }
            return 0;
        }

        public void WriteInt(int position, int value)
        {
            if (shmPtr != IntPtr.Zero)
            {
                Marshal.WriteInt32(shmPtr + position, value);
            }
        }

        public long ReadInt64(int position)
        {
            if (shmPtr != IntPtr.Zero)
            {
                return Marshal.ReadInt64(shmPtr + position);
            }
            return 0;
        }

        public void WriteInt64(int position, long value)
        {
            if (shmPtr != IntPtr.Zero)
            {
                Marshal.WriteInt64(shmPtr + position, value);
            }
        }



        // 导入Linux的动态链接库
        private const string LIBC_LIBRARY = "libc";

        // 定义POSIX共享内存相关的API函数
        [DllImport(LIBC_LIBRARY, EntryPoint = "shm_open", SetLastError = true)]
        public static extern int ShmOpen(string name, int flags, int mode);

        [DllImport(LIBC_LIBRARY, EntryPoint = "ftruncate", SetLastError = true)]
        public static extern int FTruncate(int fd, long length);

        [DllImport(LIBC_LIBRARY, EntryPoint = "mmap", SetLastError = true)]
        public static extern IntPtr MMap(IntPtr addr, IntPtr length, int prot, int flags, int fd, long offset);

        [DllImport(LIBC_LIBRARY, EntryPoint = "munmap", SetLastError = true)]
        public static extern int MUnmap(IntPtr addr, IntPtr length);

        [DllImport(LIBC_LIBRARY, EntryPoint = "shm_unlink", SetLastError = true)]
        public static extern int ShmUnlink(string name);
    }
}
