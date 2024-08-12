
using System.Threading;

namespace linker.libs
{
    public sealed class VersionManager
    {
        private ulong version = 0;

        public bool Eq(ulong outsideVersion, out ulong insideVersion)
        {
            insideVersion = version;
            return outsideVersion == version;
        }

        public void Add()
        {
            Interlocked.Increment(ref version);
        }
    }
}
