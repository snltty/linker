using linker.messenger.relay.server;
using linker.messenger.wlist;

namespace linker.messenger.store.file.relay
{
    public class RelayNodeWhiteListStore : node.NodeWhiteListStore, IRelayServerWhiteListStore
    {
        public RelayNodeWhiteListStore(IWhiteListServerStore whiteListServerStore) : base(whiteListServerStore) { }

        public override string TypeName => "Relay";
    }
}
