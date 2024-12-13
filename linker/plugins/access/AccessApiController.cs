using linker.config;
using linker.libs.api;
using linker.libs.extends;
using linker.plugins.capi;
using linker.libs;
using linker.plugins.client;
using linker.plugins.messenger;
using MemoryPack;
using linker.plugins.access.messenger;

namespace linker.plugins.access
{
    public sealed class AccessApiController : IApiClientController
    {
        private readonly IMessengerSender sender;
        private readonly ClientSignInState clientSignInState;
        private readonly AccessDecenter accessDecenter;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public AccessApiController(IMessengerSender sender, ClientSignInState clientSignInState, AccessDecenter accessDecenter, ClientConfigTransfer clientConfigTransfer)
        {
            this.sender = sender;
            this.clientSignInState = clientSignInState;
            this.accessDecenter = accessDecenter;
            this.clientConfigTransfer = clientConfigTransfer;
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

        [ClientApiAccessAttribute(ClientApiAccess.Access)]
        public async Task<bool> SetAccess(ApiControllerParamsInfo param)
        {
            ConfigUpdateAccessInfo configUpdateAccessInfo = param.Content.DeJson<ConfigUpdateAccessInfo>();
            if (configUpdateAccessInfo.ToMachineId == clientConfigTransfer.Id)
            {
                return false;
            }
            configUpdateAccessInfo.FromMachineId = clientConfigTransfer.Id;
            MessageResponeInfo resp = await sender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)AccessMessengerIds.AccessUpdateForward,
                Payload = MemoryPackSerializer.Serialize(configUpdateAccessInfo)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


    }

    public sealed class AccessListInfo
    {
        public Dictionary<string, ClientApiAccess> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
