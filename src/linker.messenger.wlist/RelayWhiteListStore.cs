using linker.messenger.relay.server;

namespace linker.messenger.wlist
{
    internal sealed class RelayWhiteListStore : IRelayServerWhiteListStore
    {
        private readonly IWhiteListServerStore whiteListServerStore;
        public RelayWhiteListStore(IWhiteListServerStore whiteListServerStore)
        {
            this.whiteListServerStore = whiteListServerStore;
        }

        public async Task<bool> Contains(string userid, string nodeid)
        {
            var list = await whiteListServerStore.Get("Relay", userid).ConfigureAwait(false);
            return list.Contains(nodeid);
        }

        public async Task<List<string>> Get(string userid)
        {
            return await whiteListServerStore.Get("Relay", userid).ConfigureAwait(false);
        }
    }
}
