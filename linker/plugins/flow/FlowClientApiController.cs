using linker.libs.api;
using linker.config;
using MemoryPack;
using linker.client.config;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.flow.messenger;

namespace linker.plugins.flow
{
    public sealed class FlowClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;

        public FlowClientApiController(MessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;
        }

        public async Task<FlowInfo> GetFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)FlowMessengerIds.List,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<FlowInfo>(resp.Data.Span);
            }
            return new FlowInfo();
        }

    }

}
