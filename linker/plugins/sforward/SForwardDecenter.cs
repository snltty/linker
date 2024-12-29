using linker.libs;
using linker.plugins.client;
using linker.serializer;
using System.Collections.Concurrent;
using MemoryPack;
using linker.messenger.decenter;
using linker.messenger.signin;

namespace linker.plugins.sforward
{
    public sealed class SForwardDecenter : IDecenter
    {
        public string Name => "sforward";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, int> CountDic { get; } = new ConcurrentDictionary<string, int>();

        private readonly ISignInClientStore signInClientStore;
        private readonly SForwardTransfer sForwardTransfer;
        public SForwardDecenter(ISignInClientStore signInClientStore, SForwardTransfer sForwardTransfer)
        {
            this.signInClientStore = signInClientStore;
            this.sForwardTransfer = sForwardTransfer;
        }

        public void Refresh()
        {
            SyncVersion.Add();
        }

        public Memory<byte> GetData()
        {
            CountInfo info = new CountInfo { MachineId = signInClientStore.Id, Count = sForwardTransfer.Count };
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
            return Serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            CountInfo info = Serializer.Deserialize<CountInfo>(data.Span);
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<CountInfo> list = data.Select(c => Serializer.Deserialize<CountInfo>(c.Span)).ToList();
            foreach (var info in list)
            {
                CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            }
            DataVersion.Add();
        }
    }

    [MemoryPackable]
    public sealed partial class CountInfo
    {
        public string MachineId { get; set; }
        public int Count { get; set; }
    }
}
