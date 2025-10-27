namespace linker.messenger.relay.server
{
    /// <summary>
    /// 白名单接口
    /// </summary>
    public interface IRelayServerWhiteListStore
    {
        /// <summary>
        /// 获取白名单
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="machineid"></param>
        /// <returns></returns>
        public Task<List<RelayWhiteListItem>> GetNodes(string userid, string machineid);
        /// <summary>
        /// 获取白名单
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="fromMachineId"></param>
        /// <param name="toMachineId"></param>
        /// <param name="nodeid"></param>
        /// <returns></returns>
        public Task<List<double>> GetBandwidth(string userid, string fromMachineId,string toMachineId, string nodeid);
    }

    public sealed class RelayServerWhiteListStore : IRelayServerWhiteListStore
    {
        public async Task<List<RelayWhiteListItem>> GetNodes(string userid, string machineid)
        {
            return await Task.FromResult(new List<RelayWhiteListItem>());
        }
        public async Task<List<double>> GetBandwidth(string userid, string fromMachineId, string toMachineId, string nodeid)
        {
            return await Task.FromResult(new List<double>());
        }
    }

    public sealed class RelayWhiteListItem
    {
        public string[] Nodes { get; set; } = [];
        public double Bandwidth { get; set; } = 0;
    }

}
