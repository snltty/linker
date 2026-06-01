using linker.libs.extends;
using linker.messenger.reverse.server;
using linker.messenger.store.file.node;

namespace linker.messenger.store.file.reverse
{
    public sealed class ReverseServerConfigStore : NodeConfigStore<ReverseServerConfigInfo>, IReverseNodeConfigStore
    {
        public override ReverseServerConfigInfo Config => config.Data.Server.Reverse;

        private readonly FileConfig config;
        public ReverseServerConfigStore(FileConfig config) : base(config)
        {
            this.config = config;

            if (string.IsNullOrWhiteSpace(config.Data.Server.SForward.ShareKey) == false)
            {
                config.Data.Server.Reverse = config.Data.Server.SForward.ToJson().DeJson<ReverseServerConfigInfo>();
                config.Data.Server.SForward.ShareKey = string.Empty;
                Confirm();
            }
        }

    }
}
