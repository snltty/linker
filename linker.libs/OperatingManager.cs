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
        private readonly ConcurrentDictionary<string, bool> dicOperating = new ConcurrentDictionary<string, bool>();

        public bool StartOperation(string key)
        {
            return dicOperating.TryAdd(key, true);
        }
        public void StopOperation(string key)
        {
            dicOperating.TryRemove(key, out _);
        }

    }
}
