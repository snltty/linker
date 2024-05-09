namespace cmonitor.share.lib
{
    //1 attr + 8 version + 4 klen + key + 4 vlen + val
    public interface IShareMemory
    {
        bool Init();

        byte ReadByte(int position);
        void WriteByte(int position, byte value);
        int ReadInt(int position);
        void WriteInt(int position, int value);

        long ReadInt64(int position);
        void WriteInt64(int position, long value);

        void ReadArray(int position, byte[] bytes, int offset, int length);
        void WriteArray(int position, byte[] data, int offset, int length);
    }
}
