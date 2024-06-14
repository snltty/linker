using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using common.libs.api;
using common.libs.extends;
using cmonitor.client;
using cmonitor.server;
using MemoryPack;
using cmonitor.server.sapi;
using cmonitor.client.capi;
using cmonitor.client.config;

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
            signCaching.TryRemove(param.Content, out _);

            return true;

        }
        public Config Config(ApiControllerParamsInfo param)
        {
            return config;
        }
    }

    public sealed class SignInClientApiController : IApiClientController
    {
        private readonly RunningConfig runningConfig;
        private readonly Config config;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientSignInTransfer clientSignInTransfer;
        private readonly MessengerSender messengerSender;

        public SignInClientApiController(RunningConfig runningConfig, Config config, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer, MessengerSender messengerSender)
        {
            this.runningConfig = runningConfig;
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.clientSignInTransfer = clientSignInTransfer;
            this.messengerSender = messengerSender;
        }

        public object Config(ApiControllerParamsInfo param)
        {
            return new { Common = config.Data.Common, Client = config.Data.Client, Running = runningConfig.Data };
        }
        public void Set(ApiControllerParamsInfo param)
        {
            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            clientSignInTransfer.UpdateName(info.Name, info.GroupId);
        }
        public async Task<bool> SetServers(ApiControllerParamsInfo param)
        {
            ConfigSetServersInfo configUpdateServersInfo = param.Content.DeJson<ConfigSetServersInfo>();


            clientSignInTransfer.UpdateServers(configUpdateServersInfo.List);
            if (configUpdateServersInfo.Sync)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)SignInMessengerIds.ServersForward,
                    Payload = MemoryPackSerializer.Serialize(configUpdateServersInfo.List)
                });
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


        public async Task<bool> SetName(ApiControllerParamsInfo param)
        {
            ConfigSetNameInfo info = param.Content.DeJson<ConfigSetNameInfo>();


            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.NameForward,
                Payload = MemoryPackSerializer.Serialize(info)
            });
            if (info.Id == config.Data.Client.Id)
            {
                clientSignInTransfer.UpdateName(info.NewName);
            }
            return true;
        }
    }

    public sealed partial class ConfigSetServersInfo
    {
        public bool Sync { get; set; }
        public ClientServerInfo[] List { get; set; } = Array.Empty<ClientServerInfo>();
    }

    [MemoryPackable]
    public sealed partial class ConfigSetNameInfo
    {
        public string Id { get; set; }
        public string NewName { get; set; }
    }

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
    }
}
