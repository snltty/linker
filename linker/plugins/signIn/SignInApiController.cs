using linker.config;
using linker.plugins.signin.messenger;
using linker.libs.api;
using linker.libs.extends;
using linker.client;
using linker.server;
using MemoryPack;
using linker.client.capi;
using linker.client.config;
using System.Diagnostics;
using linker.libs;

namespace linker.plugins.signin
{
    public sealed class SignInClientApiController : IApiClientController
    {
        private readonly RunningConfig runningConfig;
        private readonly ConfigWrap config;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientSignInTransfer clientSignInTransfer;
        private readonly MessengerSender messengerSender;

        public SignInClientApiController(RunningConfig runningConfig, ConfigWrap config, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer, MessengerSender messengerSender)
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
            ClientServerInfo[] servers = param.Content.DeJson<ClientServerInfo[]>();
            await clientSignInTransfer.UpdateServers(servers);
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
            }).ConfigureAwait(false);
        }
        public async Task<SignInListResponseInfo> List(ApiControllerParamsInfo param)
        {
            SignInListRequestInfo request = param.Content.DeJson<SignInListRequestInfo>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.List,
                Payload = MemoryPackSerializer.Serialize(request)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<SignInListResponseInfo>(resp.Data.Span);
            }
            return new SignInListResponseInfo { };
        }
        public async Task<SignInIdsResponseInfo> Ids(ApiControllerParamsInfo param)
        {
            SignInIdsRequestInfo request = param.Content.DeJson<SignInIdsRequestInfo>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Ids,
                Payload = MemoryPackSerializer.Serialize(request)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<SignInIdsResponseInfo>(resp.Data.Span);
            }
            return new SignInIdsResponseInfo { };
        }

        public async Task<bool> SetName(ApiControllerParamsInfo param)
        {
            ConfigSetNameInfo info = param.Content.DeJson<ConfigSetNameInfo>();


            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.NameForward,
                Payload = MemoryPackSerializer.Serialize(info)
            }).ConfigureAwait(false);
            if (info.Id == config.Data.Client.Id)
            {
                clientSignInTransfer.UpdateName(info.NewName);
            }
            return true;
        }

        public bool Install(ApiControllerParamsInfo param)
        {
            ConfigInstallInfo info = param.Content.DeJson<ConfigInstallInfo>();

            config.Data.Client.Name = info.Name;
            config.Data.Client.GroupId = info.GroupId;
            config.Data.Client.CApi.WebPort = info.Web;
            config.Data.Client.CApi.ApiPort = info.Api;
            config.Data.Client.CApi.ApiPassword = info.Password;
            config.Data.Common.Modes = new string[] { "client" };
            config.Save();

            if (info.Restart)
            {
                try
                {
                    CommandHelper.Execute(Process.GetCurrentProcess().MainModule.FileName, string.Empty);
                    Environment.Exit(0);
                }
                catch (Exception)
                {
                }
            }

            return true;
        }
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

    public sealed class ConfigInstallInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public int Api { get; set; }
        public int Web { get; set; }
        public string Password { get; set; }
        public bool Restart { get; set; }
    }
}
