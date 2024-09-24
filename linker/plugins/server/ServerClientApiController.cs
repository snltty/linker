using linker.libs.api;
using linker.config;
using MemoryPack;
using linker.client.config;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.server.messenger;

namespace linker.plugins.server
{
    public sealed class ServerClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;

        public ServerClientApiController(MessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;
        }

        public async Task<ServerFlowInfo> GetFlows(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ServerMessengerIds.Flow,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<ServerFlowInfo>(resp.Data.Span);
            }
            return new ServerFlowInfo();
        }

    }

}
