using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;

namespace linker.messenger.access
{
    public sealed class AccessDecenter : IDecenter
    {
        public string Name => "access";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();

        public Dictionary<string, AccessValue> Accesss { get; } = new Dictionary<string, AccessValue>();

        private readonly ISignInClientStore signInClientStore;
        private readonly IAccessStore accessStore;
        private readonly ISerializer serializer;
        public AccessDecenter(SignInClientState signInClientState, ISignInClientStore signInClientStore, IAccessStore accessStore, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.accessStore = accessStore;
            this.serializer = serializer;

            signInClientState.NetworkEnabledHandle += (times) => SyncVersion.Add();
            accessStore.OnChanged += SyncVersion.Add;
           
        }
        public void Refresh()
        {
            SyncVersion.Add();
        }
        public Memory<byte> GetData()
        {
            AccessInfo info = new AccessInfo { MachineId = signInClientStore.Id, Access = accessStore.Access };
            Accesss[info.MachineId] = info.Access;
            DataVersion.Add();
            return serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            AccessInfo access = serializer.Deserialize<AccessInfo>(data.Span);
            Accesss[access.MachineId] = access.Access;
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<AccessInfo> list = data.Select(c => serializer.Deserialize<AccessInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                Accesss[item.MachineId] = item.Access;
            }
            DataVersion.Add();
        }
    }
}
