using linker.messenger.reverse.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.reverse
{
    public class ReverseServerMasterDenyStore : NodeMasterDenyStore, IReverseServerMasterDenyStore
    {
        public override string StoreName => "reverse";
        public ReverseServerMasterDenyStore(Storefactory storefactory) :base(storefactory)
        { }
    }
}
