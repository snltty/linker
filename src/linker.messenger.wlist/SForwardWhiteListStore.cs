using linker.messenger.sforward.server;

namespace linker.messenger.wlist
{
    internal sealed class SForwardWhiteListStore : ISForwardServerWhiteListStore
    {
        private readonly IWhiteListServerStore whiteListServerStore;
        public SForwardWhiteListStore(IWhiteListServerStore whiteListServerStore)
        {
            this.whiteListServerStore = whiteListServerStore;
        }

        public async Task<bool> Contains(string userid, string nodeid)
        {
            var list = await whiteListServerStore.Get("SForward", userid).ConfigureAwait(false);
            return list.Contains(nodeid);
        }

        public async Task<List<string>> Get(string userid)
        {
            return await whiteListServerStore.Get("SForward", userid).ConfigureAwait(false);
        }
    }
}
