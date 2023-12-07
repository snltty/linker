using System;
using System.Runtime.InteropServices;

namespace cmonitor.libs
{
    internal sealed class ShareMemoryMacOS : IShareMemory
    {
        private string key;
        private int length;
        private int itemSize;

        long shmSize;
        IntPtr shmPtr;

        public ShareMemoryMacOS(string key, int length, int itemSize)
        {
            this.key = key;
            this.length = length;
            this.itemSize = itemSize;
        }

        public bool Init()
        {
            try
            {
                int shmFd = ShmOpen(key, 0x1, 0x1B6);
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

                shmPtr = MMap(IntPtr.Zero, (UIntPtr)shmSize, 0x3, 0x1 /* MAP_SHARED */, shmFd, 0);
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



        // 导入 macOS 的动态链接库
        private const string LIB_SYSTEM_LIBRARY = "/usr/lib/libSystem.dylib";

        // 定义 POSIX 共享内存相关的 API 函数
        [DllImport(LIB_SYSTEM_LIBRARY, EntryPoint = "shm_open", SetLastError = true)]
        public static extern int ShmOpen(string name, int oflag, int mode);

        [DllImport(LIB_SYSTEM_LIBRARY, EntryPoint = "ftruncate", SetLastError = true)]
        public static extern int FTruncate(int fd, long length);

        [DllImport(LIB_SYSTEM_LIBRARY, EntryPoint = "mmap", SetLastError = true)]
        public static extern IntPtr MMap(IntPtr addr, UIntPtr length, int prot, int flags, int fd, long offset);

        [DllImport(LIB_SYSTEM_LIBRARY, EntryPoint = "munmap", SetLastError = true)]
        public static extern int MUnmap(IntPtr addr, UIntPtr length);

        [DllImport(LIB_SYSTEM_LIBRARY, EntryPoint = "shm_unlink", SetLastError = true)]
        public static extern int ShmUnlink(string name);
    }
}
