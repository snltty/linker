
using System.Threading;

namespace linker.libs
{
    public sealed class VersionManager
    {
        private ulong version = 0;

        public ulong Value => version;

        public bool Eq(ulong outsideVersion, out ulong insideVersion)
        {
            insideVersion = version;
            return outsideVersion == version;
        }
        public bool LessThan(ulong outsideVersion, out ulong insideVersion)
        {
            insideVersion = version;
            return outsideVersion < version;
        }

        public void Increment()
        {
            if (Interlocked.Increment(ref version) > ulong.MaxValue - ushort.MaxValue)
            {
                Interlocked.Exchange(ref version, 1);
            }
        }

        public bool Restore()
        {
            return Interlocked.Exchange(ref version, 0) > 0;
        }
    }
}
