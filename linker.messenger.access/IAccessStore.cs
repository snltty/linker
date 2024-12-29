namespace linker.messenger.access
{
    public interface IAccessStore
    {
        public AccessValue Access { get; }

        public Action OnChanged { get; set; }

        public void SetAccess(AccessUpdateInfo info);

        public AccessValue AssignAccess(AccessValue access);

        public bool HasAccess(AccessValue clientManagerAccess);
    }
}
