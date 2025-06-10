using System.Threading;

namespace linker.libs
{
    public sealed class NumberSpace
    {
        private ulong num;

        public NumberSpace(ulong defaultVal = 0)
        {
            num = defaultVal;
        }

        public ulong Increment()
        {
            Interlocked.CompareExchange(ref num, 0, ulong.MaxValue - 10000);
            Interlocked.Increment(ref num);
            return num;
        }
    }

    public sealed class NumberSpaceUInt32
    {
        private uint num;

        public NumberSpaceUInt32(uint defaultVal = 0)
        {
            num = defaultVal;
        }
        public uint Increment()
        {
            Interlocked.CompareExchange(ref num, 0, uint.MaxValue - 10000);
            Interlocked.Increment(ref num);
            return num;
        }
    }

}
