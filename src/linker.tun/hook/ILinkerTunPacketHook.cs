namespace linker.tun.hook
{
    /// <summary>
    /// 数据包钩子
    /// </summary>
    public interface ILinkerTunPacketHook
    {
        public string Name { get; }

        public LinkerTunPacketHookLevel ReadLevel { get; }
        public LinkerTunPacketHookLevel WriteLevel { get; }

        /// <summary>
        /// 从网卡读取到数据包后,flags 默认带 next send
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet);
        /// <summary>
        /// 写入网卡前, flasgs 默认带 next write
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="originDstIp"></param>
        /// <param name="srcId"></param>
        /// <returns>next</returns>
        public ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId);
    }

    [Flags]
    public enum LinkerTunPacketHookFlags : byte
    {
        None = 0,

        //是否继续下一个钩子
        Next = 1,

        //读取端，是否发送到对端
        Send = 2,
        //读取端，是否写回网卡
        WriteBack = 4,

        //接收端，是否写入网卡
        Write = 8
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
