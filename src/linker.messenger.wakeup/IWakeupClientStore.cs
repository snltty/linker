namespace linker.messenger.wakeup
{
    /// <summary>
    /// 唤醒客户端存储接口
    /// </summary>
    public interface IWakeupClientStore
    {
        public IEnumerable<WakeupInfo> GetAll(WakeupSearchInfo info);
        public bool Add(WakeupInfo rule);
        public bool Remove(string id);
    }

    /// <summary>
    /// 唤醒类型
    /// </summary>
    public enum WakeupType : byte
    {
        /// <summary>
        /// 魔术包
        /// </summary>
        Wol = 1,
        /// <summary>
        /// 开关，继电器
        /// </summary>
        Switch = 2
    }

    public sealed class WakeupInfo
    {
        public string Id { get; set; }
        /// <summary>
        /// 唤醒类型
        /// </summary>
        public WakeupType Type { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 值，MAC地址或继电器COM名
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 内容，例如魔术包的IP地址或继电器的开关数据包内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 正在运行
        /// </summary>
        public bool Running { get; set; }
    }

    public sealed partial class WakeupSearchForwardInfo
    {
        public string MachineId { get; set; }
        public WakeupSearchInfo Data { get; set; }
    }
    public sealed partial class WakeupSearchInfo
    {
        public WakeupType Type { get; set; }
        public string Str { get; set; }
    }

    public sealed partial class WakeupAddForwardInfo
    {
        public string MachineId { get; set; }
        public WakeupInfo Data { get; set; }
    }
    public sealed partial class WakeupRemoveForwardInfo
    {
        public string MachineId { get; set; }
        public string Id { get; set; }
    }


    public sealed class WakeupSendInfo
    {
        public string Id { get; set; }
        public WakeupType Type { get; set; }
        public string Value { get; set; }
        public string Content { get; set; }

        public int Ms { get; set; }
    }
    public sealed partial class WakeupSendForwardInfo
    {
        public string MachineId { get; set; }
        public WakeupSendInfo Data { get; set; }
    }
}
