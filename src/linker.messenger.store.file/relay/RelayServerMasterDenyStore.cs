using linker.messenger.relay.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.relay
{
    public class RelayServerMasterDenyStore : NodeMasterDenyStore, IRelayServerMasterDenyStore
    {
        public override string StoreName => "relay";
        public RelayServerMasterDenyStore(Storefactory storefactory) : base(storefactory)
        { }
    }
}
