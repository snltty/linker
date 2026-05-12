using linker.messenger.node;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 白名单接口
    /// </summary>
    public interface IRelayServerWhiteListStore : INodeWhiteListStore
    {
    }

    public sealed class RelayServerWhiteListStore : IRelayServerWhiteListStore
    {
        public string TypeName => "Relay";

        public Task<List<NodeWhiteListInfo>> GetNodes(string userid, string machineid)
        {
            return Task.FromResult(new List<NodeWhiteListInfo>());
        }
        public Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return Task.FromResult(new List<double>());
        }
    }
}
