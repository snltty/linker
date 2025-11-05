using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.forward
{
    /// <summary>
    /// 端口转发的分布式数据
    /// </summary>
    public sealed class ForwardDecenter : IDecenter
    {
        public string Name => "forward";
        public VersionManager PushVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public bool Force => CountDic.Count < 2;
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
        public void Refresh()
        {
            PushVersion.Increment();
        }

        public Memory<byte> GetData()
        {
            return serializer.Serialize(new ForwardCountInfo { MachineId = signInClientStore.Id, Count = forwardTransfer.Count });
        }
        public void AddData(Memory<byte> data)
        {
            ForwardCountInfo info = serializer.Deserialize<ForwardCountInfo>(data.Span);
            CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
        }
        public void AddData(List<ReadOnlyMemory<byte>> data)
        {
            List<ForwardCountInfo> list = data.Select(c => serializer.Deserialize<ForwardCountInfo>(c.Span)).ToList();
            foreach (var info in list)
            {
                Console.WriteLine($"{info.MachineId}->{info.Count}");

                CountDic.AddOrUpdate(info.MachineId, info.Count, (a, b) => info.Count);
            };
        }
        public void ClearData()
        {
            CountDic.Clear();
        }
        public void ProcData()
        {
        }
    }
}
