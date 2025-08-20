using linker.messenger.relay.server;

namespace linker.messenger.cdkey
{
    internal class RelayCdkeyStore : IRelayServerCdkeyStore
    {
        private readonly ICdkeyServerStore cdkeyServerStore;
        public RelayCdkeyStore(ICdkeyServerStore cdkeyServerStore)
        {
            this.cdkeyServerStore = cdkeyServerStore;
        }
        public async Task<List<RelayCdkeyInfo>> GetAvailable(string userid)
        {
            return (await cdkeyServerStore.GetAvailable(userid, "Relay")).Select(c => new RelayCdkeyInfo { Bandwidth = c.Bandwidth, Id = c.Id, LastBytes = c.LastBytes }).ToList();
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
