namespace linker.messenger.sforward.server
{
    /// <summary>
    /// 白名单接口
    /// </summary>
    public interface ISForwardServerWhiteListStore
    {
        /// <summary>
        /// 获取白名单
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public Task<List<SForwardWhiteListItem>> GetNodes(string userid, string machineid);
    }

    public sealed class SForwardServerWhiteListStore : ISForwardServerWhiteListStore
    {
        public async Task<List<SForwardWhiteListItem>> GetNodes(string userid, string machineid)
        {
            return await Task.FromResult(new List<SForwardWhiteListItem>());
        }
    }

    public sealed class SForwardWhiteListItem
    {
        public string[] Nodes { get; set; } = [];
        public double Bandwidth { get; set; } = 0;
    }
}
