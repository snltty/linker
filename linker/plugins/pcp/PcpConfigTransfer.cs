using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.decenter;
using linker.tunnel.connection;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.pcp
{
    public sealed class PcpConfigTransfer : IDecenter
    {
        public string Name => "pcp";
        public VersionManager DataVersion { get; } = new VersionManager();

        private ConcurrentDictionary<string, List<string>> history = new ConcurrentDictionary<string, List<string>>();
        private readonly RunningConfig runningConfig;
        private readonly FileConfig fileConfig;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public PcpConfigTransfer(RunningConfig runningConfig, FileConfig fileConfig, ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer)
        {
            this.runningConfig = runningConfig;
            this.fileConfig = fileConfig;
            this.clientSignInState = clientSignInState;
            this.clientConfigTransfer = clientConfigTransfer;

            clientSignInState.NetworkEnabledHandle += (times) => DataVersion.Add();
        }

        public void AddHistory(ITunnelConnection connection)
        {
            if (connection.Connected && connection.Type == TunnelType.P2P && runningConfig.Data.TunnelHistory.History.Contains(connection.RemoteMachineId) == false)
            {
                runningConfig.Data.TunnelHistory.History.Add(connection.RemoteMachineId);
                runningConfig.Data.Update();
                DataVersion.Add();
            }
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
            HistoryDecenterInfo historyDecenterInfo = new HistoryDecenterInfo { MachineId = clientConfigTransfer.Id, List = runningConfig.Data.TunnelHistory.History };
            history.AddOrUpdate(historyDecenterInfo.MachineId, historyDecenterInfo.List, (a, b) => historyDecenterInfo.List);
            return MemoryPackSerializer.Serialize(historyDecenterInfo);
        }
        public void SetData(Memory<byte> data)
        {
            HistoryDecenterInfo historyDecenterInfo = MemoryPackSerializer.Deserialize<HistoryDecenterInfo>(data.Span);
            history.AddOrUpdate(historyDecenterInfo.MachineId, historyDecenterInfo.List, (a, b) => historyDecenterInfo.List);
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<HistoryDecenterInfo> list = data.Select(c => MemoryPackSerializer.Deserialize<HistoryDecenterInfo>(c.Span)).ToList();
            foreach (var historyDecenterInfo in list)
            {
                history.AddOrUpdate(historyDecenterInfo.MachineId, historyDecenterInfo.List, (a, b) => historyDecenterInfo.List);
            }
        }

    }

    [MemoryPackable]
    public sealed partial class HistoryDecenterInfo
    {
        public string MachineId { get; set; }
        public List<string> List { get; set; }
    }
}
