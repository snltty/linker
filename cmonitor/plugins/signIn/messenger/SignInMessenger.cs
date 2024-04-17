using cmonitor.config;
using cmonitor.server;
using common.libs;
using MemoryPack;

namespace cmonitor.plugins.signin.messenger
{
    public sealed class SignInMessenger : IMessenger
    {
        private readonly SignCaching signCaching;
        private readonly Config config;
        public SignInMessenger(SignCaching signCaching, Config config)
        {
            this.signCaching = signCaching;
            this.config = config;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            SignInfo info = MemoryPackSerializer.Deserialize<SignInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (info.Version == config.Data.Version)
            {
                signCaching.Sign(connection, info);
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }
    }


}
