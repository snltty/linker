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

        public async Task<List<NodeWhiteListInfo>> GetNodes(string userid, string machineid)
        {
            return await Task.FromResult(new List<NodeWhiteListInfo>()).ConfigureAwait(false);
        }
        public async Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return await Task.FromResult(new List<double>()).ConfigureAwait(false);
        }
    }
}
