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
                info = new FpsInfo { Flag = 1, Time = Environment.TickCount };
                dic.TryAdd(name, info);
            }
            bool res = info.Flag == 1 && Environment.TickCount - info.Time > 1000 / fps;
            if (res)
            {
                Interlocked.Exchange(ref info.Flag, 0);
                info.Time = Environment.TickCount;
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
            public int Time = Environment.TickCount;
        }
    }
}
