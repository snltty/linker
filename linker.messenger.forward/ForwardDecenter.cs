using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.forward
{
    public sealed class ForwardDecenter : IDecenter
    {
        public string Name => "forward";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, int> CountDic { get; } = new ConcurrentDictionary<string, int>();


        private readonly ISignInClientStore signInClientStore;
        private readonly ForwardTransfer forwardTransfer;
        private readonly ISerializer serializer;
        public ForwardDecenter(ISignInClientStore signInClientStore, ForwardTransfer forwardTransfer, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.forwardTransfer = forwardTransfer;
            this.serializer = serializer;

            forwardTransfer.OnReset += CountDic.Clear;
            forwardTransfer.OnChanged += Refresh;
          
        }

        public Memory<byte> GetData()
        {
            ForwardCountInfo info = new ForwardCountInfo { MachineId = signInClientStore.Id, Count = forwardTransfer.Count };
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
            return serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            ForwardCountInfo info = serializer.Deserialize<ForwardCountInfo>(data.Span);
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            DataVersion.Add();
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<ForwardCountInfo> list = data.Select(c => serializer.Deserialize<ForwardCountInfo>(c.Span)).ToList();
            foreach (var info in list)
            {
                CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            }
            DataVersion.Add();
        }
        public void Refresh()
        {
            SyncVersion.Add();
        }
    }
}
