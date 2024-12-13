using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using MemoryPack;

namespace linker.plugins.access
{
    public sealed class AccessDecenter : IDecenter
    {
        public string Name => "access";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();

        public Dictionary<string, ClientApiAccess> Accesss { get; } = new Dictionary<string, ClientApiAccess>();

        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly AccessTransfer accessTransfer;
        public AccessDecenter(ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer, AccessTransfer accessTransfer)
        {
            this.clientConfigTransfer = clientConfigTransfer;
            this.accessTransfer = accessTransfer;

            clientSignInState.NetworkEnabledHandle += (times) => SyncVersion.Add();
            accessTransfer.OnChanged += SyncVersion.Add;
        }
        public void Refresh()
        {
            SyncVersion.Add();
        }
        public Memory<byte> GetData()
        {
            ConfigAccessInfo info = new ConfigAccessInfo { MachineId = clientConfigTransfer.Id, Access = accessTransfer.Access };
            Accesss[info.MachineId] = info.Access;
            DataVersion.Add();
            return MemoryPackSerializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            ConfigAccessInfo access = MemoryPackSerializer.Deserialize<ConfigAccessInfo>(data.Span);
            Accesss[access.MachineId] = access.Access;
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<ConfigAccessInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<ConfigAccessInfo>(c.Span)).ToList();
            foreach (var item in list)
            {
                Accesss[item.MachineId] = item.Access;
            }
            DataVersion.Add();
        }
    }
}
