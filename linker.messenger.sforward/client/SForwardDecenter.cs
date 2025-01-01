using linker.libs;
using System.Collections.Concurrent;
using linker.messenger.decenter;
using linker.messenger.signin;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardDecenter : IDecenter
    {
        public string Name => "sforward";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, int> CountDic { get; } = new ConcurrentDictionary<string, int>();

        private readonly ISignInClientStore signInClientStore;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        public SForwardDecenter(ISignInClientStore signInClientStore, ISForwardClientStore sForwardClientStore, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.sForwardClientStore = sForwardClientStore;
            this.serializer = serializer;
        }

        public void Refresh()
        {
            SyncVersion.Add();
        }

        public Memory<byte> GetData()
        {
            SForwardCountInfo info = new SForwardCountInfo { MachineId = signInClientStore.Id, Count = sForwardClientStore.Count() };
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
            return serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            SForwardCountInfo info = serializer.Deserialize<SForwardCountInfo>(data.Span);
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<SForwardCountInfo> list = data.Select(c => serializer.Deserialize<SForwardCountInfo>(c.Span)).ToList();
            foreach (var info in list)
            {
                CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            }
            DataVersion.Add();
        }
    }

  
}
