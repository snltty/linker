using System;
using System.Collections.Concurrent;
using System.Threading;

namespace common.libs
{
    public sealed class FpsHelper
    {
        private ConcurrentDictionary<string, FpsInfo> dic = new ConcurrentDictionary<string, FpsInfo>();

        public bool Acquire(string name, int fps)
        {
            if (dic.TryGetValue(name, out FpsInfo info) == false)
            {
                info = new FpsInfo { Flag = 0, Time = Environment.TickCount };
                dic.TryAdd(name, info);
            }

            long time = info.Time;
            info.Time = Environment.TickCount;

            return Interlocked.CompareExchange(ref info.Flag, 0, 1) == 1 && Environment.TickCount - time > 1000 / fps;
        }

        public void Release(string name)
        {
            if (dic.TryGetValue(name, out FpsInfo info))
            {
                Interlocked.Exchange(ref info.Flag, 1);
            }
        }

        sealed class FpsInfo
        {
            public int Flag = 1;
            public int Time = Environment.TickCount;
        }
    }
}
