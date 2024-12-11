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
using linker.libs;
using linker.plugins.access;

namespace linker.plugins.updater
{
    public sealed class UpdaterClientApiController : IApiClientController
    {
        private readonly IMessengerSender messengerSender;
        private readonly UpdaterClientTransfer updaterTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;
        private readonly AccessTransfer accessTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly UpdaterCommonTransfer updaterCommonTransfer;

        public UpdaterClientApiController(IMessengerSender messengerSender, UpdaterClientTransfer updaterTransfer, ClientSignInState clientSignInState, FileConfig config, RunningConfig runningConfig, AccessTransfer accessTransfer, ClientConfigTransfer clientConfigTransfer, UpdaterCommonTransfer updaterCommonTransfer)
        {
            this.messengerSender = messengerSender;
            this.updaterTransfer = updaterTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;
            this.accessTransfer = accessTransfer;
            this.clientConfigTransfer = clientConfigTransfer;
            this.updaterCommonTransfer = updaterCommonTransfer;
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

        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public void SetInterval(ApiControllerParamsInfo param)
        {
            updaterCommonTransfer.SetInterval(int.Parse(param.Content));
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
                Payload = MemoryPackSerializer.Serialize(new UpdaterConfirmServerInfo { SecretKey = updaterTransfer.SecretKey, Version = param.Content })
            });
        }
        [ClientApiAccessAttribute(ClientApiAccess.UpdateServer)]
        public async Task ExitServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ExitServer,
                Payload = MemoryPackSerializer.Serialize(new UpdaterConfirmServerInfo { SecretKey = updaterTransfer.SecretKey, Version = string.Empty })
            });
        }

        public UpdateInfo GetCurrent(ApiControllerParamsInfo param)
        {
            var updaters = updaterTransfer.Get();
            if (updaters.TryGetValue(clientConfigTransfer.Id, out UpdateInfo info))
            {
                return info;
            }
            return new UpdateInfo { };
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

            if (confirm.All || confirm.GroupAll || confirm.MachineId != clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.UpdateOther) == false)
                {
                    return false;
                }

                confirm.SecretKey = updaterTransfer.SecretKey;
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ConfirmForward,
                    Payload = MemoryPackSerializer.Serialize(confirm)
                });
                if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                {
                    return false;
                }
            }
            if (confirm.MachineId == clientConfigTransfer.Id || confirm.All || confirm.GroupAll)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.UpdateSelf) == false)
                {
                    return false;
                }
                updaterTransfer.Confirm(confirm.Version);
            }
            return true;
        }
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.UpdateSelf) == false) return false;

                Environment.Exit(1);
            }
            else
            {
                if (accessTransfer.HasAccess(ClientApiAccess.UpdateOther) == false) return false;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ExitForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                });
            }
            return true;
        }


        public async Task Subscribe(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.SubscribeForward
            });
        }
        public async Task Check(ApiControllerParamsInfo param)
        {
            if(param.Content != clientConfigTransfer.Id)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.CheckForward,
                    Payload = string.IsNullOrWhiteSpace(param.Content) ? Helper.EmptyArray : MemoryPackSerializer.Serialize(param.Content)
                });
            }
            
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == clientConfigTransfer.Id)
            {
                updaterTransfer.Check();
            }
        }
    }

    public sealed class UpdaterListInfo
    {
        public ConcurrentDictionary<string, UpdateInfo> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
