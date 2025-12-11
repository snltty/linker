using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;

namespace linker.messenger.signin
{
    public sealed class SignInApiController : IApiController
    {
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;

        public SignInApiController(SignInClientState signInClientState, ISignInClientStore signInClientStore, IMessengerSender messengerSender, SignInClientTransfer signInClientTransfer, ISerializer serializer, IAccessStore accessStore)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.messengerSender = messengerSender;
            this.signInClientTransfer = signInClientTransfer;
            this.serializer = serializer;
            this.accessStore = accessStore;
        }

        public void Set(ApiControllerParamsInfo param)
        {
            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            if (accessStore.HasAccess(AccessValue.RenameSelf))
            {
                signInClientStore.SetName(info.Name);
            }
            if (accessStore.HasAccess(AccessValue.Group))
            {
                signInClientStore.SetGroups(info.Groups);
            }

            signInClientTransfer.ReSignIn();
        }

        public async Task<bool> SetName(ApiControllerParamsInfo param)
        {
            SignInConfigSetNameInfo info = param.Content.DeJson<SignInConfigSetNameInfo>();

            if (info.Id == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.RenameSelf) == false) return false;
                signInClientStore.SetName(info.NewName);
                signInClientTransfer.ReSignIn();
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.RenameSelf) == false) return false;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)SignInMessengerIds.SetNameForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }

        [Access(AccessValue.Group)]
        public void SetGroups(ApiControllerParamsInfo param)
        {
            SignInClientGroupInfo[] info = param.Content.DeJson<SignInClientGroupInfo[]>();
            signInClientStore.SetGroups(info);
            //signInClientTransfer.ReSignIn();
        }

        [Access(AccessValue.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            SignInClientServerInfo servers = param.Content.DeJson<SignInClientServerInfo>();
            signInClientStore.SetServer(servers);
            //signInClientTransfer.ReSignIn();
            return true;
        }

        public SignInClientState Info(ApiControllerParamsInfo param)
        {
            return signInClientState;
        }

        [Access(AccessValue.Config)]
        public async Task Del(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Delete,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
        }
        public async Task SetOrder(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.SetOrder,
                Payload = serializer.Serialize(param.Content.DeJson<string[]>())
            }).ConfigureAwait(false);
        }
        public async Task<SignInListResponseInfo> List(ApiControllerParamsInfo param)
        {
            SignInListRequestInfo request = param.Content.DeJson<SignInListRequestInfo>();
            return await signInClientTransfer.List(request);
        }
        public async Task<SignInIdsResponseInfo> Ids(ApiControllerParamsInfo param)
        {
            SignInIdsRequestInfo request = param.Content.DeJson<SignInIdsRequestInfo>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Ids,
                Payload = serializer.Serialize(request)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<SignInIdsResponseInfo>(resp.Data.Span);
            }
            return new SignInIdsResponseInfo { };
        }
        public async Task<List<SignInNamesResponseItemInfo>> Names(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Names
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<SignInNamesResponseItemInfo>>(resp.Data.Span);
            }
            return new List<SignInNamesResponseItemInfo>();
        }

        public async Task<List<SignInUserIdsResponseItemInfo>> UserIds(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.UserIds,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<SignInUserIdsResponseItemInfo>>(resp.Data.Span);
            }
            return [];
        }

        public async Task CheckSuper(ApiControllerParamsInfo param)
        {
            await signInClientTransfer.CheckSuper().ConfigureAwait(false);
        }


      
    }

  

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public SignInClientGroupInfo[] Groups { get; set; }
    }

}
