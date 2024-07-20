using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using MemoryPack;
using linker.plugins.sforward.messenger;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;

namespace linker.plugins.sforward
{
    public sealed class SForwardClientApiController : IApiClientController
    {
        private readonly SForwardTransfer forwardTransfer;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public SForwardClientApiController(SForwardTransfer forwardTransfer, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
        }


        public string GetSecretKey(ApiControllerParamsInfo param)
        {
            return forwardTransfer.GetSecretKey();
        }
        public void SetSecretKey(ApiControllerParamsInfo param)
        {
            forwardTransfer.SetSecretKey(param.Content);
        }

        public List<SForwardInfo> Get(ApiControllerParamsInfo param)
        {
            return forwardTransfer.Get();
        }
        public async Task<List<SForwardRemoteInfo>> GetRemote(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.GetForward,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<List<SForwardRemoteInfo>>(resp.Data.Span);
            }
            return new List<SForwardRemoteInfo>();
        }

        public bool Add(ApiControllerParamsInfo param)
        {
            SForwardInfo info = param.Content.DeJson<SForwardInfo>();
            return forwardTransfer.Add(info);
        }

        public bool Remove(ApiControllerParamsInfo param)
        {
            if (uint.TryParse(param.Content, out uint id))
            {
                return forwardTransfer.Remove(id);
            }
            return false;
        }
        public bool TestLocal(ApiControllerParamsInfo param)
        {
            forwardTransfer.TestLocal();
            return true;
        }

    }
}
