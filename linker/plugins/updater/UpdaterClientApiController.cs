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
        private readonly RunningConfig runningConfig;

        public UpdaterClientApiController(MessengerSender messengerSender, UpdaterClientTransfer updaterTransfer, ClientSignInState clientSignInState, FileConfig config, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.updaterTransfer = updaterTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;
        }

        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public string GetSecretKey(ApiControllerParamsInfo param)
        {
            return updaterTransfer.GetSecretKey();
        }

        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public void SetSecretKey(ApiControllerParamsInfo param)
        {
            updaterTransfer.SetSecretKey(param.Content);
        }

        public UpdateInfo GetCurrent(ApiControllerParamsInfo param)
        {
            var updaters = updaterTransfer.Get();
            if (updaters.TryGetValue(config.Data.Client.Id, out UpdateInfo info))
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

        [ClientApiAccessAttribute(ClientApiAccess.UpdateServer)]
        public async Task ConfirmServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ConfirmServer,
                Payload = MemoryPackSerializer.Serialize(new UpdaterConfirmServerInfo { SecretKey = config.Data.Client.Updater.SecretKey, Version = param.Content })
            });
        }
        [ClientApiAccessAttribute(ClientApiAccess.UpdateServer)]
        public async Task ExitServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ExitServer,
                Payload = MemoryPackSerializer.Serialize(new UpdaterConfirmServerInfo { SecretKey = config.Data.Client.Updater.SecretKey, Version = string.Empty })
            });
        }


        public UpdaterListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (updaterTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new UpdaterListInfo
                {
                    List = updaterTransfer.Get(),
                    HashCode = version
                };
            }
            return new UpdaterListInfo { HashCode = version };

        }
        public async Task<bool> Confirm(ApiControllerParamsInfo param)
        {
            UpdaterConfirmInfo confirm = param.Content.DeJson<UpdaterConfirmInfo>();

            if (confirm.MachineId != config.Data.Client.Id)
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.UpdateSelf) == false) return false;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ConfirmForward,
                    Payload = MemoryPackSerializer.Serialize(confirm)
                });
            }
            if (confirm.MachineId == config.Data.Client.Id || confirm.All)
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.UpdateOther) == false) return false;

                updaterTransfer.Confirm(confirm.Version);
            }

            return true;
        }
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == config.Data.Client.Id)
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.UpdateSelf) == false) return false;

                Environment.Exit(1);
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.UpdateOther) == false) return false;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ExitForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
            return true;
        }
    }

    public sealed class UpdaterListInfo
    {
        public ConcurrentDictionary<string, UpdateInfo> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
