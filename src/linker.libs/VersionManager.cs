
using System.Collections.Concurrent;
using System.Threading;

namespace linker.libs
{
    public sealed class VersionManager
    {
        private ulong version;

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

    public sealed class VersionMultipleManager
    {
        private readonly ConcurrentDictionary<string, VersionInfo> dicVersion = new ConcurrentDictionary<string, VersionInfo>();

        public void Increment(string key)
        {
            if (dicVersion.TryGetValue(key, out VersionInfo version) == false)
            {
                version = new VersionInfo();
                dicVersion.TryAdd(key, version);
            }
            Interlocked.Increment(ref version.value);

        }
        public bool HasValueChange(string key)
        {
            if (dicVersion.TryGetValue(key, out VersionInfo version))
            {
                return Interlocked.Exchange(ref version.oldValue, version.value) != version.value;
            }
            return false;
        }

        sealed class VersionInfo
        {
            public uint value;
            public uint oldValue;
        }
    }
}
