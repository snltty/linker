using linker.libs;
using linker.libs.api;
using linker.libs.extends;

namespace linker.messenger.signin
{
    public sealed class SignInApiController : IApiController
    {
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISerializer serializer;

        public SignInApiController( SignInClientState signInClientState, ISignInClientStore signInClientStore, IMessengerSender messengerSender,SignInClientTransfer signInClientTransfer, ISerializer serializer)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.messengerSender = messengerSender;
            this.signInClientTransfer = signInClientTransfer;
            this.serializer = serializer;
        }

        public void Set(ApiControllerParamsInfo param)
        {
            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            signInClientStore.SetName(info.Name);
            signInClientStore.SetGroup(info.Groups[0]);
            signInClientTransfer.ReSignIn();
        }

        public async Task<bool> SetName(ApiControllerParamsInfo param)
        {
            SignInConfigSetNameInfo info = param.Content.DeJson<SignInConfigSetNameInfo>();

            if (info.Id == signInClientStore.Id)
            {
                signInClientStore.SetName(info.NewName);
                signInClientTransfer.ReSignIn();
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)SignInMessengerIds.SetNameForward,
                    Payload = serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }
        public void SetGroups(ApiControllerParamsInfo param)
        {
            SignInClientGroupInfo[] info = param.Content.DeJson<SignInClientGroupInfo[]>();
            signInClientStore.SetGroup(info[0]);
            //signInClientTransfer.ReSignIn();
        }

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
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.List,
                Payload = serializer.Serialize(request)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<SignInListResponseInfo>(resp.Data.Span);
            }
            return new SignInListResponseInfo { };
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

    }

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public SignInClientGroupInfo[] Groups { get; set; }
    }

}
