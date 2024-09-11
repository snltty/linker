using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.config.messenger;
using linker.plugins.messenger;
using linker.plugins.tunnel.messenger;
using MemoryPack;

namespace linker.plugins.config
{
    public sealed class AccessTransfer
    {
        public VersionManager Version { get; } = new VersionManager();
        private Dictionary<string, ClientApiAccess> accesss = new Dictionary<string, ClientApiAccess>();

        private readonly FileConfig fileConfig;
        private readonly MessengerSender sender;
        private readonly ClientSignInState clientSignInState;
        public AccessTransfer(FileConfig fileConfig, MessengerSender sender, ClientSignInState clientSignInState)
        {
            this.fileConfig = fileConfig;
            this.sender = sender;
            this.clientSignInState = clientSignInState;

            clientSignInState.NetworkEnabledHandle += (times) => Sync();
        }

        public Dictionary<string, ClientApiAccess> GetAccesss()
        {
            return accesss;
        }

        public ConfigAccessInfo GetAccess()
        {
            return new ConfigAccessInfo { MachineId = fileConfig.Data.Client.Id, Access = fileConfig.Data.Client.Access }; ;
        }
        public void SetAccess(ConfigAccessInfo access)
        {
            accesss[access.MachineId] = access.Access;
            Version.Add();
            //Sync();
        }
        public void SetAccess(ConfigUpdateAccessInfo info)
        {
            //我的权限删掉它的权限==0，说明它至少拥有我的全部权限，我是它的子集，它有权管我
            if (accesss.TryGetValue(info.FromMachineId, out ClientApiAccess access) && ((~access) & fileConfig.Data.Client.Access) == 0)
            {
                fileConfig.Data.Client.Access = access;
                fileConfig.Data.Update();
                Version.Add();
                Sync();
            }
        }
        public ClientApiAccess AssignAccess(ClientApiAccess access)
        {
            return fileConfig.Data.Client.Access & access;
        }

        private void Sync()
        {
            ConfigAccessInfo access = GetAccess();
            sender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ConfigMessengerIds.AccessForward,
                Timeout = 10000,
                Payload = MemoryPackSerializer.Serialize(access)
            }).ContinueWith((result) =>
            {
                if (result.Result.Code == MessageResponeCodes.OK)
                {
                    List<ConfigAccessInfo> list = MemoryPackSerializer.Deserialize<List<ConfigAccessInfo>>(result.Result.Data.Span);
                    accesss = list.ToDictionary(c => c.MachineId, d => d.Access);
                    accesss[access.MachineId] = access.Access;
                    Version.Add();
                }
            });
        }
    }
}
