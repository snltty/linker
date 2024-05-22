using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using common.libs.api;
using common.libs.extends;
using cmonitor.client;
using cmonitor.server;
using MemoryPack;
using cmonitor.server.sapi;
using cmonitor.client.capi;

namespace cmonitor.plugins.signin
{
    public sealed class SignInServerApiController : IApiServerController
    {
        private readonly SignCaching signCaching;
        private readonly Config config;
        public SignInServerApiController(SignCaching signCaching, Config config)
        {
            this.signCaching = signCaching;
            this.config = config;
        }
        public List<SignCacheInfo> List(ApiControllerParamsInfo param)
        {
            List<SignCacheInfo> caches = signCaching.Get(param.Content);
            return caches;
        }
        public bool Del(ApiControllerParamsInfo param)
        {
            signCaching.Del(param.Content);

            return true;

        }
        public Config Config(ApiControllerParamsInfo param)
        {
            return config;
        }
    }

    public sealed class SignInClientApiController : IApiClientController
    {
        private readonly Config config;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientSignInTransfer clientSignInTransfer;
        private readonly MessengerSender messengerSender;

        public SignInClientApiController(Config config, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer, MessengerSender messengerSender)
        {
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.clientSignInTransfer = clientSignInTransfer;
            this.messengerSender = messengerSender;
        }

        public Config Config(ApiControllerParamsInfo param)
        {
            return config;
        }
        public void Set(ApiControllerParamsInfo param)
        {
            string name = config.Data.Client.Name;
            string gid = config.Data.Client.GroupId;

            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            config.Data.Client.Name = info.Name;
            config.Data.Client.GroupId = info.GroupId;
            config.Save();

            if (name != config.Data.Client.Name || gid != config.Data.Client.GroupId)
            {
                clientSignInTransfer.SignOut();
                _ = clientSignInTransfer.SignIn();
            }
        }
        public bool SetServers(ApiControllerParamsInfo param)
        {
            string server = config.Data.Client.Server;

            config.Data.Client.Servers = param.Content.DeJson<ClientServerInfo[]>();
            config.Save();

            if (server != config.Data.Client.Server)
            {
                clientSignInTransfer.SignOut();
                _ = clientSignInTransfer.SignIn();
            }

            return true;
        }

        public ClientSignInState Info(ApiControllerParamsInfo param)
        {
            return clientSignInState;
        }
        public async Task Del(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Delete,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            });
        }
        public async Task<SignInListResponseInfo> List(ApiControllerParamsInfo param)
        {
            SignInListRequestInfo request = param.Content.DeJson<SignInListRequestInfo>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.List,
                Payload = MemoryPackSerializer.Serialize(request)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<SignInListResponseInfo>(resp.Data.Span);
            }
            return new SignInListResponseInfo { };
        }


        public async Task<bool> UpdateName(ApiControllerParamsInfo param)
        {
            ConfigUpdateNameInfo info = param.Content.DeJson<ConfigUpdateNameInfo>();


            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.UpdateNameForward,
                Payload = MemoryPackSerializer.Serialize(info)
            });

            if (info.OldName == config.Data.Client.Name)
            {
                config.Data.Client.Name = info.NewName;
                config.Save();
                clientSignInTransfer.SignOut();
                _ = clientSignInTransfer.SignIn();
            }
            return true;
        }
    }

    [MemoryPackable]
    public sealed partial class ConfigUpdateNameInfo
    {
        public string OldName { get; set; }
        public string NewName { get; set; }
    }

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
    }
}
