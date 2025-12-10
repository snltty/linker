using linker.messenger.node;

namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 白名单接口
    /// </summary>
    public interface ISForwardServerWhiteListStore : INodeWhiteListStore
    {
    }

    public sealed class SForwardServerWhiteListStore : ISForwardServerWhiteListStore
    {
        public string TypeName => "SForward";

        public async Task<List<NodeWhiteListInfo>> GetNodes(string userid, string machineid)
        {
            return await Task.FromResult(new List<NodeWhiteListInfo>());
        }
        public async Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return await Task.FromResult(new List<double>());
        }
    }
}
