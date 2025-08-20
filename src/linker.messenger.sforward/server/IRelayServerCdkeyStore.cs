namespace linker.messenger.sforward.server
{
    public interface ISForwardServerCdkeyStore
    {
        public Task<List<SForwardCdkeyInfo>> GetAvailable(string userid,string target);

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

    public sealed class SForwardServerCdkeyStore : ISForwardServerCdkeyStore
    {
        public async Task<List<SForwardCdkeyInfo>> GetAvailable(string userid, string target)
        {
            return await Task.FromResult(new List<SForwardCdkeyInfo>());
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

    public partial class SForwardCdkeyInfo
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
