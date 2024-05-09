using System;

namespace cmonitor.libs
{
    //1 attr + 8 version + 4 klen + key + 4 vlen + val
    internal interface IShareMemory
    {
        public bool Init();

        public byte ReadByte(int position);
        public void WriteByte(int position, byte value);
        public int ReadInt(int position);
        public void WriteInt(int position, int value);

        public long ReadInt64(int position);
        public void WriteInt64(int position, long value);

        public void ReadArray(int position, byte[] bytes, int offset, int length);
        public void WriteArray(int position, byte[] data, int offset, int length);
    }
}
