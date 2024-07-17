using linker.server;
using linker.libs.api;
using linker.client;
using linker.client.capi;
using linker.config;
using linker.plugins.updater.messenger;
using MemoryPack;
using System.Collections.Concurrent;

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

        public ConcurrentDictionary<string, UpdateInfo> Get(ApiControllerParamsInfo param)
        {
            return updaterTransfer.Get();
        }
        public async Task Confirm(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == config.Data.Client.Id)
            {
                updaterTransfer.Confirm();
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ConfirmForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
        }
        public async Task Exit(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == config.Data.Client.Id)
            {
                updaterTransfer.Exit();
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ExitForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
        }
    }
}
