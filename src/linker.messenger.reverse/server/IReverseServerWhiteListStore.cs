using linker.messenger.node;

namespace linker.messenger.reverse.server
{
    /// <summary>
    /// 白名单接口
    /// </summary>
    public interface IReverseServerWhiteListStore : INodeWhiteListStore
    {
    }

    public sealed class ReverseServerWhiteListStore : IReverseServerWhiteListStore
    {
        public string TypeName => "Reverse";

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
