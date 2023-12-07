namespace cmonitor.service
{
    /// <summary>
    /// 消息接口
    /// </summary>
    public interface IMessenger
    {
    }

    /// <summary>
    /// 消息id范围
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessengerIdRangeAttribute : Attribute
    {
        /// <summary>
        /// 最小
        /// </summary>
        public ushort Min { get; set; }
        /// <summary>
        /// 最大
        /// </summary>
        public ushort Max { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public MessengerIdRangeAttribute(ushort min, ushort max)
        {
            Min = min;
            Max = max;
        }
    }
    /// <summary>
    /// 消息id
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MessengerIdAttribute : Attribute
    {
        /// <summary>
        /// id
        /// </summary>
        public ushort Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public MessengerIdAttribute(ushort id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// 消息
    /// </summary>

    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class MessengerIdEnumAttribute : Attribute
    {
    }
}
