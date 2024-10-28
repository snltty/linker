using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using linker.plugins.messenger;
using MemoryPack;

namespace linker.plugins.access
{
    public sealed class AccessTransfer : IDecenter
    {
        public string Name => "access";
        public VersionManager DataVersion { get; } = new VersionManager();

        public VersionManager Version { get; } = new VersionManager();

        private Dictionary<string, ClientApiAccess> accesss = new Dictionary<string, ClientApiAccess>();

        private readonly FileConfig fileConfig;
        private readonly IMessengerSender sender;
        private readonly ClientSignInState clientSignInState;
        public AccessTransfer(FileConfig fileConfig, IMessengerSender sender, ClientSignInState clientSignInState)
        {
            this.fileConfig = fileConfig;
            this.sender = sender;
            this.clientSignInState = clientSignInState;

            clientSignInState.NetworkEnabledHandle += (times) => DataVersion.Add();
        }

        public Memory<byte> GetData()
        {
            ConfigAccessInfo info = new ConfigAccessInfo { MachineId = fileConfig.Data.Client.Id, Access = fileConfig.Data.Client.Access };
            accesss[info.MachineId] = info.Access;
            Version.Add();
            return MemoryPackSerializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            ConfigAccessInfo access = MemoryPackSerializer.Deserialize<ConfigAccessInfo>(data.Span);
            accesss[access.MachineId] = access.Access;
            Version.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<ConfigAccessInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<ConfigAccessInfo>(c.Span)).ToList();
            accesss = list.ToDictionary(c => c.MachineId, d => d.Access);
            accesss[fileConfig.Data.Client.Id] = fileConfig.Data.Client.Access;
            Version.Add();
        }

        public void RefreshConfig()
        {
            DataVersion.Add();
        }

        public Dictionary<string, ClientApiAccess> GetAccesss()
        {
            return accesss;
        }
        public void SetAccess(ConfigUpdateAccessInfo info)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access},my access {(ulong)fileConfig.Data.Client.Access}");

            //我的权限删掉它的权限==0，说明它至少拥有我的全部权限，我是它的子集，它有权管我
            if (accesss.TryGetValue(info.FromMachineId, out ClientApiAccess access) && (~access & fileConfig.Data.Client.Access) == 0)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access} success");
                fileConfig.Data.Client.Access = (ClientApiAccess)info.Access;
                fileConfig.Data.Update();
            }
            GetData();
            Version.Add();
            DataVersion.Add();
        }
        public ClientApiAccess AssignAccess(ClientApiAccess access)
        {
            return fileConfig.Data.Client.Access & access;
        }

    }
}
