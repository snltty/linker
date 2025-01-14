using linker.messenger.pcp;
using linker.tunnel.connection;

namespace linker.messenger.store.file.pcp
{
    public sealed class PcpStore : IPcpStore
    {
        public PcpHistoryInfo PcpHistory => runningConfig.Data.PcpHistory;

        private readonly RunningConfig runningConfig;
        public PcpStore(RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;
        }
        public void AddHistory(ITunnelConnection connection)
        {
            runningConfig.Data.PcpHistory.History.Add(connection.RemoteMachineId);
            runningConfig.Data.PcpHistory.History = runningConfig.Data.PcpHistory.History.Distinct().ToList();
            runningConfig.Data.Update();
        }
    }
}
