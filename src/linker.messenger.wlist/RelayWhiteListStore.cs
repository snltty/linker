using linker.messenger.relay.server;

namespace linker.messenger.wlist
{
    internal sealed class RelayWhiteListStore : IRelayServerWhiteListStore
    {
        private readonly IWhiteListServerStore whiteListServerStore;
        public RelayWhiteListStore(IWhiteListServerStore whiteListServerStore)
        {
            this.whiteListServerStore = whiteListServerStore;
        }
        public async Task<List<RelayWhiteListItem>> GetNodes(string userid,string machineid)
        {
            return (await whiteListServerStore.Get("Relay", [userid], [machineid]).ConfigureAwait(false)).Select(c => new RelayWhiteListItem { Nodes = c.Nodes, Bandwidth = c.Bandwidth }).ToList();
        }
        public async Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return (await whiteListServerStore.Get("Relay", [userid], [fromMachineId, toMachineId]).ConfigureAwait(false)).Where(c => c.Nodes.Contains(nodeid) || c.Nodes.Contains("*")).Select(c => c.Bandwidth).ToList();
        }
    }
}
