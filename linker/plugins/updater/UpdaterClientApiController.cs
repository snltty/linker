using linker.server;
using linker.libs.api;
using linker.client;
using linker.client.capi;
using linker.config;
using linker.plugins.updater.messenger;
using MemoryPack;

namespace linker.plugins.updater
{
    public sealed class UpdaterClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly UpdaterTransfer updaterTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;

        public UpdaterClientApiController(MessengerSender messengerSender, UpdaterTransfer updaterTransfer, ClientSignInState clientSignInState, FileConfig config)
        {
            this.messengerSender = messengerSender;
            this.updaterTransfer = updaterTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
        }

        public UpdateInfo Get(ApiControllerParamsInfo param)
        {
            return updaterTransfer.Get();
        }
        public async Task Update(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == config.Data.Client.Id)
            {
                updaterTransfer.Update();
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.UpdateForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
        }
    }
}
