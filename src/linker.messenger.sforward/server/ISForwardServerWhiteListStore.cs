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
        public Task<List<string>> Get(string userid);
        /// <summary>
        /// 是否存在于白名单
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="nodeid"></param>
        /// <returns></returns>
        public Task<bool> Contains(string userid, string nodeid);
    }

    public sealed class SForwardServerWhiteListStore : ISForwardServerWhiteListStore
    {
        public async Task<bool> Contains(string userid, string nodeid)
        {
            return await Task.FromResult(false);
        }

        public async Task<List<string>> Get(string userid)
        {
            return await Task.FromResult(new List<string>());
        }
    }
}
