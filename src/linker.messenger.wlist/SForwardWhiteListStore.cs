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
        public async Task<List<SForwardWhiteListItem>> GetNodes(string userid,string machineid)
        {
            return (await whiteListServerStore.Get("SForward", [userid], [machineid]).ConfigureAwait(false)).Select(c => new SForwardWhiteListItem { Nodes = c.Nodes, Bandwidth = c.Bandwidth }).ToList();
        }
    }
}
