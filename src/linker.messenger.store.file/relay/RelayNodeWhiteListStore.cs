using linker.messenger.relay.server;
using linker.messenger.wlist;

namespace linker.messenger.store.file.relay
{
    public class RelayNodeWhiteListStore : node.NodeWhiteListStore, IRelayServerWhiteListStore
    {
        public override string TypeName => "Relay";
        public RelayNodeWhiteListStore(IWhiteListServerStore whiteListServerStore) : base(whiteListServerStore) { }

      
    }
}
