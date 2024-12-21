using linker.libs;
using linker.messenger;
using linker.messenger.signin;
using linker.messenger.relay.messenger;
using linker.messenger.relay.client;
using linker.messenger.relay.server;
using linker.messenger.relay.server.validator;

namespace linker.plugins.relay.messenger
{
    /// <summary>
    /// 中继客户端
    /// </summary>
    public sealed class PlusRelayClientMessenger : RelayClientMessenger, IMessenger
    {
        private readonly RelayClientTransfer relayTransfer;
        public PlusRelayClientMessenger(RelayClientTransfer relayTransfer, ISerializer serializer) : base(relayTransfer, serializer)
        {
            this.relayTransfer = relayTransfer;
        }
    }

    /// <summary>
    /// 中继服务端
    /// </summary>
    public sealed class PlusRelayServerMessenger : RelayServerMessenger, IMessenger
    {
        public PlusRelayServerMessenger(IMessengerSender messengerSender, SignCaching signCaching, RelayServerMasterTransfer relayServerTransfer, RelayServerValidatorTransfer relayValidatorTransfer, ISerializer serializer)
            : base(messengerSender, signCaching, serializer, relayServerTransfer, relayValidatorTransfer)
        {
        }
    }
}
