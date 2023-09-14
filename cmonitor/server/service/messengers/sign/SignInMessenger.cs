using common.libs;
using MemoryPack;

namespace cmonitor.server.service.messengers.sign
{
    public sealed class SignInMessenger : IMessenger
    {
        private readonly SignCaching signCaching;
        public SignInMessenger(SignCaching signCaching)
        {
            this.signCaching = signCaching;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            signCaching.Sign(connection, MemoryPackSerializer.Deserialize<SignInfo>(connection.ReceiveRequestWrap.Payload.Span));
            connection.Write(Helper.TrueArray);
        }
    }


}
