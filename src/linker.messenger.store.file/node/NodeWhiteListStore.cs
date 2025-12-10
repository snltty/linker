using linker.messenger.node;
using linker.messenger.wlist;

namespace linker.messenger.store.file.node
{
    public class NodeWhiteListStore : INodeWhiteListStore
    {
        public virtual string TypeName => string.Empty;


        private readonly IWhiteListServerStore whiteListServerStore;
        public NodeWhiteListStore(IWhiteListServerStore whiteListServerStore)
        {
            this.whiteListServerStore = whiteListServerStore;
        }
        public async Task<List<NodeWhiteListInfo>> GetNodes(string userid,string machineid)
        {
            return (await whiteListServerStore.Get(TypeName, [userid], [machineid]).ConfigureAwait(false)).Select(c => new NodeWhiteListInfo { Nodes = c.Nodes, Bandwidth = c.Bandwidth }).ToList();
        }
        public async Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return (await whiteListServerStore.Get(TypeName, [userid], [fromMachineId, toMachineId]).ConfigureAwait(false)).Where(c => c.Nodes.Contains(nodeid) || c.Nodes.Contains("*")).Select(c => c.Bandwidth).ToList();
        }

    }
}
