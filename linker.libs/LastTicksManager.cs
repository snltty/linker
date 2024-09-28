using System;

namespace linker.libs
{
    public sealed class LastTicksManager
    {
        private long ticks = Environment.TickCount64;

        /// <summary>
        /// 当前值
        /// </summary>
        public long Value => ticks;

        
        /// <summary>
        /// 差小于等于
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool DiffLessEqual(long ms)
        {
            return Environment.TickCount64 - ticks <= ms;
        }
        /// <summary>
        /// 差大于
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool DiffGreater(long ms)
        {
            return Environment.TickCount64 - ticks > ms;
        }
        /// <summary>
        /// 等于
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool Equal(long ms)
        {
            return ticks == ms;
        }

        /// <summary>
        /// 差值
        /// </summary>
        /// <returns></returns>
        public long Diff()
        {
            return Environment.TickCount64 - ticks;
        }
        /// <summary>
        /// 超时
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool Expired(long ms)
        {
            return ticks == 0 || Environment.TickCount64 - ticks > ms;
        }
        /// <summary>
        /// 不等于
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool HasValue()
        {
            return ticks != 0;
        }

        /// <summary>
        /// 更新值
        /// </summary>
        public void Update()
        {
            ticks = Environment.TickCount64;
        }
        /// <summary>
        /// 清除值
        /// </summary>
        public void Clear()
        {
            ticks = 0;
        }

    }

}
