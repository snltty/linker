using linker.messenger.reverse.server;
using linker.messenger.wlist;

namespace linker.messenger.store.file.reverse
{
    public class ReverseNodeWhiteListStore : node.NodeWhiteListStore, IReverseServerWhiteListStore
    {
        public override string TypeName => "Reverse";
        public ReverseNodeWhiteListStore(IWhiteListServerStore whiteListServerStore) : base(whiteListServerStore) { }

       
    }
}
