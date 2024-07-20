using linker.libs.api;
using linker.config;
using linker.plugins.updater.messenger;
using MemoryPack;
using System.Collections.Concurrent;
using linker.plugins.updater.config;
using linker.libs.extends;
using linker.client.config;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;

namespace linker.plugins.updater
{
    public sealed class UpdaterClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly UpdaterClientTransfer updaterTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly UpdaterClientTransfer updaterClientTransfer;
        private readonly RunningConfig runningConfig;

        public UpdaterClientApiController(MessengerSender messengerSender, UpdaterClientTransfer updaterTransfer, ClientSignInState clientSignInState, FileConfig config, UpdaterClientTransfer updaterClientTransfer, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.updaterTransfer = updaterTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.updaterClientTransfer = updaterClientTransfer;
            this.runningConfig = runningConfig;
        }

        public string GetSecretKey(ApiControllerParamsInfo param)
        {
            return updaterClientTransfer.GetSecretKey();
        }
        public void SetSecretKey(ApiControllerParamsInfo param)
        {
            updaterClientTransfer.SetSecretKey(param.Content);
        }

        public UpdateInfo GetCurrent(ApiControllerParamsInfo param)
        {
            var updaters = updaterTransfer.Get();
            if(updaters.TryGetValue(config.Data.Client.Id,out UpdateInfo info))
            {
                return info;
            }
            return new UpdateInfo { };
        }
        public async Task<UpdateInfo> GetServer(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.UpdateServer,
            });
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return MemoryPackSerializer.Deserialize<UpdateInfo>(resp.Data.Span);
            }
            return new UpdateInfo();
        }
        public async Task ConfirmServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ConfirmServer,
                Payload = MemoryPackSerializer.Serialize(new UpdaterConfirmServerInfo { SecretKey = runningConfig.Data.UpdaterSecretKey, Version = param.Content })
            });
        }
        public async Task ExitServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ExitServer,
                Payload = MemoryPackSerializer.Serialize(new UpdaterConfirmServerInfo { SecretKey = runningConfig.Data.UpdaterSecretKey, Version = string.Empty })
            });
        }


        public ConcurrentDictionary<string, UpdateInfo> Get(ApiControllerParamsInfo param)
        {
            return updaterTransfer.Get();
        }
        public async Task Confirm(ApiControllerParamsInfo param)
        {
            UpdaterConfirmInfo confirm = param.Content.DeJson<UpdaterConfirmInfo>();

            if (string.IsNullOrWhiteSpace(confirm.MachineId) || confirm.MachineId == config.Data.Client.Id)
            {
                updaterTransfer.Confirm(confirm.Version);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ConfirmForward,
                    Payload = MemoryPackSerializer.Serialize(confirm)
                });
            }
        }
        public async Task Exit(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == config.Data.Client.Id)
            {
                Environment.Exit(1);
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
