namespace linker.messenger.node
{
    /// <summary>
    /// 白名单接口
    /// </summary>
    public interface INodeWhiteListStore
    {
        public string TypeName { get; }

        /// <summary>
        /// 获取白名单
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="machineid"></param>
        /// <returns></returns>
        public Task<List<NodeWhiteListInfo>> GetNodes(string userid, string machineid);
        /// <summary>
        /// 获取白名单
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="fromMachineId"></param>
        /// <param name="toMachineId"></param>
        /// <param name="nodeid"></param>
        /// <returns></returns>
        public Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid);
    }

    public sealed class NodeWhiteListStore : INodeWhiteListStore
    {
        public string TypeName => string.Empty;

        public async Task<List<NodeWhiteListInfo>> GetNodes(string userid, string machineid)
        {
            return await Task.FromResult(new List<NodeWhiteListInfo>());
        }
        public async Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return await Task.FromResult(new List<double>());
        }
    }

    public sealed class NodeWhiteListInfo
    {
        public string[] Nodes { get; set; } = [];
        public double Bandwidth { get; set; } = 0;
    }

}
