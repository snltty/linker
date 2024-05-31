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
                info = new FpsInfo { Flag = 1, Time = Environment.TickCount64 };
                dic.TryAdd(name, info);
            }
            bool res = info.Flag == 1 && Environment.TickCount64 - info.Time > 1000 / fps;
            if (res)
            {
                Interlocked.Exchange(ref info.Flag, 0);
                info.Time = Environment.TickCount64;
            }
            return res;
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
            public long Time = Environment.TickCount64;
        }
    }
}
