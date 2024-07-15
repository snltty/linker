using linker.config;
using linker.plugins.signin.messenger;
using linker.libs.api;
using linker.libs.extends;
using linker.client;
using linker.server;
using MemoryPack;
using linker.client.capi;

namespace linker.plugins.signin
{
    public sealed class SignInClientApiController : IApiClientController
    {
        private readonly FileConfig config;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientSignInTransfer clientSignInTransfer;
        private readonly MessengerSender messengerSender;

        public SignInClientApiController(FileConfig config, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer, MessengerSender messengerSender)
        {
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.clientSignInTransfer = clientSignInTransfer;
            this.messengerSender = messengerSender;

        }

        public void Set(ApiControllerParamsInfo param)
        {
            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            clientSignInTransfer.Set(info.Name, info.GroupId);
        }
        public async Task<bool> SetName(ApiControllerParamsInfo param)
        {
            ConfigSetNameInfo info = param.Content.DeJson<ConfigSetNameInfo>();

            if (info.Id == config.Data.Client.Id)
            {
                clientSignInTransfer.SetName(info.NewName);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)SignInMessengerIds.SetNameForward,
                    Payload = MemoryPackSerializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }
        public async Task<bool> SetServers(ApiControllerParamsInfo param)
        {
            ClientServerInfo[] servers = param.Content.DeJson<ClientServerInfo[]>();
            await clientSignInTransfer.SetServers(servers);
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
