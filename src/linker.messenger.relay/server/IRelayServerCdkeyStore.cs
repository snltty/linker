namespace linker.messenger.relay.server
{
    public interface IRelayServerCdkeyStore
    {
        public Task<List<RelayCdkeyInfo>> GetAvailable(string userid);

        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public Task<bool> Traffic(Dictionary<int, long> dic);
        /// <summary>
        /// 获取剩余流量
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task<Dictionary<int, long>> GetLastBytes(List<int> ids);
    }

    public sealed class RelayServerCdkeyStore : IRelayServerCdkeyStore
    {
        public async Task<List<RelayCdkeyInfo>> GetAvailable(string userid)
        {
            return await Task.FromResult(new List<RelayCdkeyInfo>());
        }

        public async Task<Dictionary<int, long>> GetLastBytes(List<int> ids)
        {
            return await Task.FromResult(new Dictionary<int, long>());
        }

        public async Task<bool> Traffic(Dictionary<int, long> dic)
        {
            return await Task.FromResult(true);
        }
    }

    public partial class RelayCdkeyInfo
    {
        public int Id { get; set; }
        /// <summary>
        /// 带宽Mbps
        /// </summary>
        public double Bandwidth { get; set; }
        /// <summary>
        /// 剩余流量
        /// </summary>
        public long LastBytes { get; set; }
    }
}
