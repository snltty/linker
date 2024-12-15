namespace linker.messenger
{
    public interface IMessenger
    {
    }

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
