using linker.messenger.sforward.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.sforward
{
    public class SForwardServerMasterDenyStore : NodeMasterDenyStore, ISForwardServerMasterDenyStore
    {
        public override string StoreName => "sforward";
        public SForwardServerMasterDenyStore(Storefactory storefactory) :base(storefactory)
        { }
    }
}
