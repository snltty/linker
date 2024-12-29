using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;
using linker.plugins.client;
using linker.serializer;
using System.Collections.Concurrent;

namespace linker.plugins.forward
{
    public sealed class ForwardDecenter:IDecenter
    {
        public string Name => "forward";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public  ConcurrentDictionary<string, int> CountDic { get; }= new ConcurrentDictionary<string, int>();


        private readonly ISignInClientStore signInClientStore;
        private readonly ForwardTransfer forwardTransfer;
        public ForwardDecenter(ISignInClientStore signInClientStore, ForwardTransfer forwardTransfer)
        {
            this.signInClientStore = signInClientStore;
            this.forwardTransfer = forwardTransfer;
            forwardTransfer.OnReset += CountDic.Clear;
            forwardTransfer.OnChanged += SyncVersion.Add;
        }

        public Memory<byte> GetData()
        {
            CountInfo info = new CountInfo { MachineId = signInClientStore.Id, Count = forwardTransfer.Count };
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
        public void Refresh()
        {
            SyncVersion.Add();
        }
    }
}
