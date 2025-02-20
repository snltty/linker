using linker.libs;
using linker.messenger.decenter;
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.pcp
{
    public sealed class PcpDecenter : IDecenter
    {
        public string Name => "pcp";
        public VersionManager SyncVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();

        private ConcurrentDictionary<string, List<string>> history = new ConcurrentDictionary<string, List<string>>();
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IPcpStore pcpStore;
        private readonly ISerializer serializer;
        public PcpDecenter( SignInClientState signInClientState, ISignInClientStore signInClientStore, IPcpStore pcpStore, ISerializer serializer)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.pcpStore = pcpStore;
            this.serializer = serializer;

            signInClientState.OnSignInSuccess += (times) => SyncVersion.Add();
        }

        public List<string> GetNodes(string fromMachineId, string toMachineId)
        {
            if (history.TryGetValue(fromMachineId, out List<string> from) && history.TryGetValue(toMachineId, out List<string> to))
            {

            }
            return new List<string>();
        }

        public Memory<byte> GetData()
        {
            HistoryDecenterInfo historyDecenterInfo = new HistoryDecenterInfo { MachineId = signInClientStore.Id, List = pcpStore.PcpHistory.History };
            history.AddOrUpdate(historyDecenterInfo.MachineId, historyDecenterInfo.List, (a, b) => historyDecenterInfo.List);
            return serializer.Serialize(historyDecenterInfo);
        }
        public void SetData(Memory<byte> data)
        {
            HistoryDecenterInfo historyDecenterInfo = serializer.Deserialize<HistoryDecenterInfo>(data.Span);
            history.AddOrUpdate(historyDecenterInfo.MachineId, historyDecenterInfo.List, (a, b) => historyDecenterInfo.List);
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<HistoryDecenterInfo> list = data.Select(c => serializer.Deserialize<HistoryDecenterInfo>(c.Span)).ToList();
            foreach (var historyDecenterInfo in list)
            {
                history.AddOrUpdate(historyDecenterInfo.MachineId, historyDecenterInfo.List, (a, b) => historyDecenterInfo.List);
            }
        }

    }

    public sealed partial class HistoryDecenterInfo
    {
        public string MachineId { get; set; }
        public List<string> List { get; set; }
    }
}
