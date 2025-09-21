
using linker.tun.device;

namespace linker.tun.hook
{
    /// <summary>
    /// 数据包钩子
    /// </summary>
    public interface ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level { get; }

        /// <summary>
        /// 从网卡读取到数据包后
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public bool Read(ReadOnlyMemory<byte> packet);
        /// <summary>
        /// 写入网卡前
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public bool Write(string srcId, ReadOnlyMemory<byte> packet);
    }
    /// <summary>
    /// 回调处理级别
    /// </summary>
    public enum LinkerTunPacketHookLevel
    {
        /// <summary>
        /// 最低的，也是最早执行的，不要用这个
        /// </summary>
        Lowest = int.MinValue,
        Low9 = -9,
        Low8 = -8,
        Low7 = -7,
        Low6 = -6,
        Low5 = -5,
        Low4 = -4,
        Low3 = -3,
        Low2 = -2,
        Low1 = -1,
        Normal = 0,
        High1 = 1,
        High2 = 2,
        High3 = 3,
        High4 = 4,
        High5 = 5,
        High6 = 6,
        High7 = 7,
        High8 = 8,
        High9 = 9,
        /// <summary>
        /// 最高的，也是最晚执行的，不要用这个
        /// </summary>
        Highest = int.MaxValue
    }

}
