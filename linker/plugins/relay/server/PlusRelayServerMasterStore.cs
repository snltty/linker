using linker.messenger.relay.server;

namespace linker.plugins.relay.server
{
    public sealed class PlusRelayServerMasterStore : IRelayServerMasterStore
    {
        public RelayServerMasterInfo Master => relayServerConfigTransfer.Master;

        private readonly RelayServerConfigTransfer relayServerConfigTransfer;
        public PlusRelayServerMasterStore(RelayServerConfigTransfer relayServerConfigTransfer)
        {
            this.relayServerConfigTransfer = relayServerConfigTransfer;
        }

    }
}
