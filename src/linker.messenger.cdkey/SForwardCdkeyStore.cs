using linker.messenger.sforward.server;

namespace linker.messenger.cdkey
{
    internal class SForwardCdkeyStore : ISForwardServerCdkeyStore
    {
        private readonly ICdkeyServerStore cdkeyServerStore;
        public SForwardCdkeyStore(ICdkeyServerStore cdkeyServerStore)
        {
            this.cdkeyServerStore = cdkeyServerStore;
        }
        public async Task<List<SForwardCdkeyInfo>> GetAvailable(string userid,string target)
        {
            return (await cdkeyServerStore.GetAvailable(userid, "SForward")).Select(c => new SForwardCdkeyInfo { Bandwidth = c.Bandwidth, Id = c.Id, LastBytes = c.LastBytes }).ToList();
        }

        public async Task<Dictionary<int, long>> GetLastBytes(List<int> ids)
        {
            return await cdkeyServerStore.GetLastBytes(ids);
        }

        public async Task<bool> Traffic(Dictionary<int, long> dic)
        {
            return await cdkeyServerStore.Traffic(dic);
        }
    }
}
