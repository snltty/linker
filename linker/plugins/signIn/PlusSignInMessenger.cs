using MemoryPack;
using linker.plugins.client;
using linker.messenger;
using linker.libs;
using linker.messenger.signin;
using linker.plugins.signin;

namespace linker.plugins.signIn
{
    public sealed class PlusSignInClientMessenger : IMessenger
    {
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly ClientSignInTransfer clientSignInTransfer;
        public PlusSignInClientMessenger(ClientConfigTransfer clientConfigTransfer, ClientSignInTransfer clientSignInTransfer)
        {
            this.clientConfigTransfer = clientConfigTransfer;
            this.clientSignInTransfer = clientSignInTransfer;
        }

        [MessengerId((ushort)SignInMessengerIds.SetName)]
        public void Name(IConnection connection)
        {
            ConfigSetNameInfo info = MemoryPackSerializer.Deserialize<ConfigSetNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            clientConfigTransfer.SetName(info.NewName);
            clientSignInTransfer.ReSignIn();
        }

    }

    public sealed class PlusSignInServerMessenger : SignInServerMessenger, IMessenger
    {
        private readonly SignCaching signCaching;
        private readonly IMessengerSender messengerSender;

        public PlusSignInServerMessenger(SignCaching signCaching, IMessengerSender messengerSender, ISerializer serializer) : base(messengerSender,signCaching, serializer)
        {
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)SignInMessengerIds.SetNameForward)]
        public async Task NameForward(IConnection connection)
        {
            ConfigSetNameInfo info = MemoryPackSerializer.Deserialize<ConfigSetNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.Id, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                if (info.Id != connection.Id)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)SignInMessengerIds.SetName,
                        Payload = connection.ReceiveRequestWrap.Payload,
                    }).ConfigureAwait(false);
                }
            }
        }

    }
}
