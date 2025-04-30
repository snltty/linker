using System.Collections.Concurrent;
using System.Threading;

namespace linker.libs
{
    public sealed class OperatingManager
    {
        private uint operating = 0;
        public bool Operating => operating == 1;

        public bool StartOperation()
        {
            return Interlocked.CompareExchange(ref operating, 1, 0) == 0;
        }
        public void StopOperation()
        {
            Interlocked.Exchange(ref operating, 0);
        }
    }

    public sealed class OperatingMultipleManager
    {
        public ConcurrentDictionary<string, bool> StringKeyValue=> dicOperating;

        private readonly ConcurrentDictionary<string, bool> dicOperating = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<uint, bool> dicOperating1 = new ConcurrentDictionary<uint, bool>();

        public bool StartOperation(string key)
        {
            return dicOperating.TryAdd(key, true);
        }
        public void StopOperation(string key)
        {
            dicOperating.TryRemove(key, out _);
        }

        public bool StartOperation(uint key)
        {
            return dicOperating1.TryAdd(key, true);
        }
        public void StopOperation(uint key)
        {
            dicOperating1.TryRemove(key, out _);
        }
    }
}
