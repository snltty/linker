using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.sforward
{
    public sealed class SForwardDecenter : IDecenter
    {
        public string Name => "sforward";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, int> CountDic { get; } = new ConcurrentDictionary<string, int>();

        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly SForwardTransfer sForwardTransfer;
        public SForwardDecenter(ClientConfigTransfer clientConfigTransfer, SForwardTransfer sForwardTransfer)
        {
            this.clientConfigTransfer = clientConfigTransfer;
            this.sForwardTransfer = sForwardTransfer;
        }

        public void Refresh()
        {
            SyncVersion.Add();
        }

        public Memory<byte> GetData()
        {
            CountInfo info = new CountInfo { MachineId = clientConfigTransfer.Id, Count = sForwardTransfer.Count };
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
            return MemoryPackSerializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            CountInfo info = MemoryPackSerializer.Deserialize<CountInfo>(data.Span);
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<CountInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<CountInfo>(c.Span)).ToList();
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
