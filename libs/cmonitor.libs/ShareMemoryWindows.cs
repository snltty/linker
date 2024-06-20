using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace cmonitor.libs
{
    internal sealed class ShareMemoryWindows : IShareMemory
    {
        private string key;
        private int length;
        private int itemSize;
        MemoryMappedFile mmfLocal = null;
        MemoryMappedViewAccessor accessorLocal = null;

        public ShareMemoryWindows(string key, int length, int itemSize)
        {
            this.key = key;
            this.length = length;
            this.itemSize = itemSize;
        }

        public bool Init()
        {
            try
            {
                if (accessorLocal == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    mmfLocal = MemoryMappedFile.CreateOrOpen($"{key}", length * itemSize, MemoryMappedFileAccess.ReadWriteExecute, MemoryMappedFileOptions.None, HandleInheritability.None);

                    SetSecurityInfoByHandle(mmfLocal.SafeMemoryMappedFileHandle, 1, 4, null, null, null, null);

                    accessorLocal = mmfLocal.CreateViewAccessor();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
        [DllImport("advapi32.dll", EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        static extern uint SetSecurityInfoByHandle(SafeHandle handle, uint objectType, uint securityInformation, byte[] owner, byte[] group, byte[] dacl, byte[] sacl);

        public void ReadArray(int position, byte[] bytes, int offset, int length)
        {
            if (accessorLocal != null)
            {
                accessorLocal.ReadArray(position, bytes, offset, bytes.Length);
            }
        }
        public void WriteArray(int position, byte[] data, int offset, int length)
        {
            if (accessorLocal != null)
            {
                accessorLocal.WriteArray(position, data, offset, length);
            }
        }

        public byte ReadByte(int position)
        {
            if (accessorLocal != null)
            {
                return accessorLocal.ReadByte(position);
            }
            return 0;
        }
        public void WriteByte(int position, byte value)
        {
            if (accessorLocal != null)
            {
                accessorLocal.Write(position, value);
            }
        }


        public int ReadInt(int position)
        {
            if (accessorLocal != null)
            {
                return accessorLocal.ReadInt32(position);
            }
            return 0;
        }
        public void WriteInt(int position, int value)
        {
            if (accessorLocal != null)
            {
                accessorLocal.Write(position, value);
            }
        }

        public long ReadInt64(int position)
        {
            if (accessorLocal != null)
            {
                return accessorLocal.ReadInt64(position);
            }
            return 0;
        }
        public void WriteInt64(int position, long value)
        {
            if (accessorLocal != null)
            {
                accessorLocal.Write(position, value);
            }
        }


    }

}
