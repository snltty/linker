namespace linker.messenger
{
    /// <summary>
    /// 信标
    /// </summary>
    public interface IMessenger
    {
    }

    /// <summary>
    /// 给信标标记上一个编号
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MessengerIdAttribute : Attribute
    {
        public ushort Id { get; set; }
        public MessengerIdAttribute(ushort id)
        {
            Id = id;
        }
    }

}
