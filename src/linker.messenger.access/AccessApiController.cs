using linker.libs.extends;
using linker.libs;
using linker.messenger.signin;
using linker.messenger.api;
using System.Collections.Concurrent;
using System.Collections;
using linker.libs.web;

namespace linker.messenger.access
{
    public sealed class AccessApiController : IApiController
    {
        private readonly IMessengerSender sender;
        private readonly SignInClientState signInClientState;
        private readonly AccessDecenter accessDecenter;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;
        private readonly IApiStore apiStore;
        private readonly linker.messenger.api.IWebServer apiServer;

        public AccessApiController(IMessengerSender sender, SignInClientState signInClientState, AccessDecenter accessDecenter,
            ISignInClientStore signInClientStore, ISerializer serializer, IAccessStore accessStore, IApiStore apiStore, linker.messenger.api.IWebServer apiServer)
        {
            this.sender = sender;
            this.signInClientState = signInClientState;
            this.accessDecenter = accessDecenter;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.accessStore = accessStore;
            this.apiStore = apiStore;
            this.apiServer = apiServer;
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
            AccessBitsUpdateInfo configUpdateAccessInfo = param.Content.DeJson<AccessBitsUpdateInfo>();
            if (configUpdateAccessInfo.ToMachineId == signInClientStore.Id)
            {
                return false;
            }
            configUpdateAccessInfo.FromMachineId = signInClientStore.Id;
            MessageResponeInfo resp = await sender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)AccessMessengerIds.AccessStrUpdateForward,
                Payload = serializer.Serialize(configUpdateAccessInfo)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> SetApiPassword(ApiControllerParamsInfo param)
        {
            ApiPasswordUpdateInfo info = param.Content.DeJson<ApiPasswordUpdateInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.SetApiPassword) == false) return false;
                apiStore.SetApiPassword(info.Password);
                apiStore.Confirm();
                apiServer.SetPassword(info.Password);
                return true;
            }
            if (accessStore.HasAccess(AccessValue.SetApiPasswordOther) == false) return false;
            MessageResponeInfo resp = await sender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)AccessMessengerIds.SetApiPasswordForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

    }

    public sealed class AccessListInfo
    {
        public ConcurrentDictionary<string, BitArray> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
