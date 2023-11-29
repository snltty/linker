using System;
using System.IO.MemoryMappedFiles;

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
                if (accessorLocal == null)
                {
                    mmfLocal = MemoryMappedFile.CreateOrOpen($"{key}", length * itemSize);
                    accessorLocal = mmfLocal.CreateViewAccessor();
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

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
    }
}
