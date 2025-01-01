using linker.libs.api;
using linker.libs.extends;
using linker.libs;
using linker.messenger.signin;
using linker.messenger.api;

namespace linker.messenger.access
{
    public sealed class AccessApiController : IApiController
    {
        private readonly IMessengerSender sender;
        private readonly SignInClientState signInClientState;
        private readonly AccessDecenter accessDecenter;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;

        public AccessApiController(IMessengerSender sender, SignInClientState signInClientState, AccessDecenter accessDecenter, ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.sender = sender;
            this.signInClientState = signInClientState;
            this.accessDecenter = accessDecenter;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }

        public void Refresh(ApiControllerParamsInfo param)
        {
            accessDecenter.Refresh();
        }

        public AccessListInfo GetAccesss(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (accessDecenter.DataVersion.Eq(hashCode, out ulong version) == false)
            {
                return new AccessListInfo
                {
                    HashCode = version,
                    List = accessDecenter.Accesss
                };
            }
            return new AccessListInfo { HashCode = version };
        }

        [Access(AccessValue.Access)]
        public async Task<bool> SetAccess(ApiControllerParamsInfo param)
        {
            AccessUpdateInfo configUpdateAccessInfo = param.Content.DeJson<AccessUpdateInfo>();
            if (configUpdateAccessInfo.ToMachineId == signInClientStore.Id)
            {
                return false;
            }
            configUpdateAccessInfo.FromMachineId = signInClientStore.Id;
            MessageResponeInfo resp = await sender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)AccessMessengerIds.AccessUpdateForward,
                Payload = serializer.Serialize(configUpdateAccessInfo)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


    }

    public sealed class AccessListInfo
    {
        public Dictionary<string, AccessValue> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
