using linker.config;
using linker.plugins.signin.messenger;
using linker.libs.api;
using linker.libs.extends;
using MemoryPack;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.access;

namespace linker.plugins.signin
{
    public sealed class SignInClientApiController : IApiClientController
    {
        private readonly FileConfig config;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly AccessTransfer accessTransfer;
        private readonly ClientSignInTransfer clientSignInTransfer;

        public SignInClientApiController(FileConfig config, ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer, IMessengerSender messengerSender, AccessTransfer accessTransfer, ClientSignInTransfer clientSignInTransfer)
        {
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.clientConfigTransfer = clientConfigTransfer;
            this.messengerSender = messengerSender;
            this.accessTransfer = accessTransfer;
            this.clientSignInTransfer = clientSignInTransfer;
        }

        public void Set(ApiControllerParamsInfo param)
        {
            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            clientConfigTransfer.SetName(info.Name);
            clientConfigTransfer.SetGroup(info.Groups);
            clientSignInTransfer.ReSignIn();
        }

        public async Task<bool> SetName(ApiControllerParamsInfo param)
        {
            ConfigSetNameInfo info = param.Content.DeJson<ConfigSetNameInfo>();

            if (info.Id == clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.RenameSelf) == false) return false;

                clientConfigTransfer.SetName(info.NewName);
                clientSignInTransfer.ReSignIn();
            }
            else
            {
                if (accessTransfer.HasAccess(ClientApiAccess.RenameOther) == false) return false;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)SignInMessengerIds.SetNameForward,
                    Payload = MemoryPackSerializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }
        public void SetGroups(ApiControllerParamsInfo param)
        {
            ClientGroupInfo[] info = param.Content.DeJson<ClientGroupInfo[]>();
            clientConfigTransfer.SetGroup(info);
            clientSignInTransfer.ReSignIn();
        }

        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            ClientServerInfo servers = param.Content.DeJson<ClientServerInfo>();
            clientConfigTransfer.SetServer([servers]);
            clientSignInTransfer.ReSignIn();
            return true;
        }

        public ClientSignInState Info(ApiControllerParamsInfo param)
        {
            return clientSignInState;
        }

        [ClientApiAccessAttribute(ClientApiAccess.RenameOther)]
        public async Task Del(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Delete,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            }).ConfigureAwait(false);
        }
        public async Task SetOrder(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.SetOrder,
                Payload = MemoryPackSerializer.Serialize(param.Content.DeJson<string[]>())
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
        public ClientGroupInfo[] Groups { get; set; }
    }

}
