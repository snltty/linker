using linker.messenger.relay.server;
using linker.messenger.sforward.server;
using linker.messenger.wlist;

namespace linker.messenger.store.file.sforward
{
    public class SForwardNodeWhiteListStore : node.NodeWhiteListStore, ISForwardServerWhiteListStore
    {
        public override string TypeName => "SForward";
        public SForwardNodeWhiteListStore(IWhiteListServerStore whiteListServerStore) : base(whiteListServerStore) { }

       
    }
}
