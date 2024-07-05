using linker.libs.api;
using linker.libs.extends;
using linker.client.capi;
using linker.client.config;
using linker.server;
using linker.client;
using linker.plugins.sforward.messenger;
using MemoryPack;

namespace linker.plugins.sforward
{
    public sealed class SForwardClientApiController : IApiClientController
    {
        private readonly SForwardTransfer forwardTransfer;
        private readonly RunningConfig runningConfig;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public SForwardClientApiController(SForwardTransfer forwardTransfer, RunningConfig runningConfig, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.forwardTransfer = forwardTransfer;
            this.runningConfig = runningConfig;
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
