using linker.messenger.mesh;
using linker.tunnel.connection;

namespace linker.messenger.store.file.mesh
{
    public sealed class MeshStore : IMeshStore
    {
        public MeshHistoryInfo MeshHistory => runningConfig.Data.MeshHistory;

        private readonly RunningConfig runningConfig;
        public MeshStore(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
        }
        public void AddHistory(ITunnelConnection connection)
        {
            if (connection.Type != TunnelType.P2P || connection.TransactionId == "mesh")
            {
                return;
            }
            runningConfig.Data.MeshHistory.History.Add(connection.RemoteMachineId);
            runningConfig.Data.MeshHistory.History = runningConfig.Data.MeshHistory.History.Distinct().ToList();
            runningConfig.Data.Update();
        }

        public void RemoveHistorys(List<string> historys)
        {
            runningConfig.Data.MeshHistory.History.RemoveAll(h => historys.Contains(h));
            runningConfig.Data.Update();
        }
    }
}
