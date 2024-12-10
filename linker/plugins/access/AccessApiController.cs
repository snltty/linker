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
        private readonly FileConfig config;
        private readonly IMessengerSender sender;
        private readonly ClientSignInState clientSignInState;
        private readonly AccessTransfer accessTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public AccessApiController(FileConfig config, IMessengerSender sender, ClientSignInState clientSignInState, AccessTransfer accessTransfer, ClientConfigTransfer clientConfigTransfer)
        {
            this.config = config;
            this.sender = sender;
            this.clientSignInState = clientSignInState;
            this.accessTransfer = accessTransfer;
            this.clientConfigTransfer = clientConfigTransfer;
        }

        public void Refresh(ApiControllerParamsInfo param)
        {
            accessTransfer.RefreshConfig();
        }

        public AccessListInfo GetAccesss(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (accessTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new AccessListInfo
                {

                    HashCode = version,
                    List = accessTransfer.GetAccesss()

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
