using cmonitor.client;
using cmonitor.config;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;

namespace cmonitor.plugins.signin.messenger
{
    public sealed class SignInClientMessenger : IMessenger
    {
        private readonly Config config;
        private readonly ClientSignInTransfer clientSignInTransfer;
        public SignInClientMessenger(Config config, ClientSignInTransfer clientSignInTransfer)
        {
            this.config = config;
            this.clientSignInTransfer = clientSignInTransfer;
        }

        [MessengerId((ushort)SignInMessengerIds.Update)]
        public void Update(IConnection connection)
        {

        }

        [MessengerId((ushort)SignInMessengerIds.UpdateName)]
        public void UpdateName(IConnection connection)
        {
            ConfigUpdateNameInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            config.Data.Client.Name = info.NewName;
            config.Save();

            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();
        }
    }

    public sealed class SignInServerMessenger : IMessenger
    {
        private readonly SignCaching signCaching;
        private readonly Config config;
        private readonly MessengerSender messengerSender;

        public SignInServerMessenger(SignCaching signCaching, Config config, MessengerSender messengerSender)
        {
            this.signCaching = signCaching;
            this.config = config;
            this.messengerSender = messengerSender;
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


        [MessengerId((ushort)SignInMessengerIds.List)]
        public void List(IConnection connection)
        {
            SignInListRequestInfo request = MemoryPackSerializer.Deserialize<SignInListRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.Get(connection.Name, out SignCacheInfo cache))
            {
                List<SignCacheInfo> list = signCaching.Get(cache.GroupId).OrderByDescending(c => c.MachineName).OrderByDescending(c => c.LastSignIn).OrderByDescending(c => c.Version).ToList();
                int count = list.Count;
                list = list.Skip((request.Page - 1) * request.Size).Take(request.Size).ToList();

                SignInListResponseInfo response = new SignInListResponseInfo { Request = request, Count = count, List = list };

                connection.Write(MemoryPackSerializer.Serialize(response));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Delete)]
        public void Delete(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(name, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                signCaching.Del(name);
            }
        }

        [MessengerId((ushort)SignInMessengerIds.UpdateNameForward)]
        public async Task UpdateNameForward(IConnection connection)
        {
            ConfigUpdateNameInfo info = MemoryPackSerializer.Deserialize<ConfigUpdateNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(info.OldName, out SignCacheInfo cache) && signCaching.Get(connection.Name, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                if(info.OldName != connection.Name)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)SignInMessengerIds.UpdateName,
                        Payload = connection.ReceiveRequestWrap.Payload,
                    });
                }
                signCaching.Del(info.OldName);
            }
        }

    }

    [MemoryPackable]
    public sealed partial class SignInListRequestInfo
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string GroupId { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SignInListResponseInfo
    {
        public SignInListRequestInfo Request { get; set; } = new SignInListRequestInfo();
        public int Count { get; set; }
        public List<SignCacheInfo> List { get; set; } = new List<SignCacheInfo>();
    }
}
