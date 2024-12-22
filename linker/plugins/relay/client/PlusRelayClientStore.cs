using linker.messenger;
using linker.messenger.relay.client;
using linker.messenger.relay.client.transport;
using linker.plugins.client;
using linker.plugins.resolver;
using System.Security.Cryptography.X509Certificates;

namespace linker.plugins.relay.client
{
    public sealed class PlusRelayClientStore : IRelayClientStore
    {
        public byte Flag => (byte)(ResolverType.Relay);

        public X509Certificate2 Certificate => clientConfigTransfer.Certificate;

        public IConnection SigninConnection => clientSignInState.Connection;

        public string SecretKey => relayClientConfigTransfer.Server.SecretKey;

        public bool Disabled => relayClientConfigTransfer.Server.Disabled;

        public bool SSL => relayClientConfigTransfer.Server.SSL;

        public RelayClientType RelayType => relayClientConfigTransfer.Server.RelayType;

        private readonly RelayClientConfigTransfer relayClientConfigTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientConfigTransfer clientConfigTransfer;
        public PlusRelayClientStore(RelayClientConfigTransfer relayClientConfigTransfer, ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer)
        {
            this.relayClientConfigTransfer = relayClientConfigTransfer;
            this.clientSignInState = clientSignInState;
            this.clientConfigTransfer = clientConfigTransfer;
        }
    }
}
