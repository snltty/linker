using System;

namespace linker.libs
{
    public sealed class LastTicksManager
    {
        private long ticks = Environment.TickCount64;

        public long Value => ticks;

        public void Update()
        {
            ticks = Environment.TickCount64;
        }
        public bool Less(long ms)
        {
            return Environment.TickCount64 - ticks <= ms;
        }
        public bool Greater(long ms)
        {
            return Environment.TickCount64 - ticks > ms;
        }
        public bool Equal(long ms)
        {
            return ticks == ms;
        }
        public bool NotEqual(long ms)
        {
            return ticks != ms;
        }

        public long Diff()
        {
            return Environment.TickCount64 - ticks;
        }
        public bool Timeout(long ms)
        {
            return ticks == 0 || Environment.TickCount64 - ticks > ms;
        }
        public void Clear()
        {
            ticks = 0;
        }

    }

}
