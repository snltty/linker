using linker.libs.extends;
using linker.libs;
using linker.messenger.signin;
using linker.libs.web;

namespace linker.messenger.updater
{
    /// <summary>
    /// 更新控制器
    /// </summary>
    public sealed class UpdaterApiController : IApiController
    {
        private readonly IMessengerSender messengerSender;
        private readonly UpdaterClientTransfer updaterTransfer;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IUpdaterCommonStore updaterCommonTransfer;
        private readonly ISerializer serializer;
        private readonly IUpdaterClientStore updaterClientStore;

        public UpdaterApiController(IMessengerSender messengerSender, UpdaterClientTransfer updaterTransfer, SignInClientState signInClientState, ISignInClientStore signInClientStore,
            IUpdaterCommonStore updaterCommonTransfer, ISerializer serializer, IUpdaterClientStore updaterClientStore)
        {
            this.messengerSender = messengerSender;
            this.updaterTransfer = updaterTransfer;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.updaterCommonTransfer = updaterCommonTransfer;
            this.serializer = serializer;
            this.updaterClientStore = updaterClientStore;
        }

        public UpdaterConfigClientInfo GetSecretKey(ApiControllerParamsInfo param)
        {
            return updaterClientStore.Info;
        }

        /// <summary>
        /// 设置是否同步到服务器
        /// </summary>
        /// <param name="param"></param>
        public void SetSync2Server(ApiControllerParamsInfo param)
        {
            updaterClientStore.SetSync2Server(bool.Parse(param.Content));
        }
        /// <summary>
        /// 设置更新间隔
        /// </summary>
        /// <param name="param"></param>
        public void SetInterval(ApiControllerParamsInfo param)
        {
            updaterCommonTransfer.SetInterval(int.Parse(param.Content));
        }

        /// <summary>
        /// 获取服务器更新信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<UpdaterInfo170> GetServer(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.UpdateServer170,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<UpdaterInfo170>(resp.Data.Span);
            }
            return new UpdaterInfo170();
        }
        /// <summary>
        /// 获取服务器更新信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<UpdaterInfo> GetMsg(ApiControllerParamsInfo param)
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.Msg,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<UpdaterInfo>(resp.Data.Span);
            }
            return new UpdaterInfo();
        }
        /// <summary>
        /// 确认更新服务端
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task ConfirmServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ConfirmServer,
                Payload = serializer.Serialize(new UpdaterConfirmServerInfo {  Version = param.Content })
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// 重启服务端
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task ExitServer(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.ExitServer,
                Payload = serializer.Serialize(new UpdaterConfirmServerInfo { Version = string.Empty })
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 获取当前客户端更新信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public UpdaterInfo170 GetCurrent(ApiControllerParamsInfo param)
        {
            var updaters = updaterTransfer.Get();
            if (updaters.TryGetValue(signInClientStore.Id, out UpdaterInfo170 info))
            {
                return info;
            }
            return new UpdaterInfo170 { };
        }
        /// <summary>
        /// 获取列表更新信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 确认更新
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Confirm(ApiControllerParamsInfo param)
        {
            UpdaterConfirmInfo confirm = param.Content.DeJson<UpdaterConfirmInfo>();

            if (confirm.All || confirm.GroupAll || confirm.MachineId != signInClientStore.Id)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ConfirmForward,
                    Payload = serializer.Serialize(confirm)
                }).ConfigureAwait(false);
                if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                {
                    return false;
                }
            }
            if (confirm.MachineId == signInClientStore.Id || confirm.All || confirm.GroupAll)
            {
                updaterTransfer.Confirm(confirm.Version);
            }
            return true;
        }
        /// <summary>
        /// 重启客户端
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == signInClientStore.Id)
            {
                Helper.AppExit(1);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.ExitForward,
                    Payload = serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
            }
            return true;
        }

        public async Task Subscribe(ApiControllerParamsInfo param)
        {
            updaterTransfer.Subscribe();
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.SubscribeForward
            }).ConfigureAwait(false);
        }
        public async Task Check(ApiControllerParamsInfo param)
        {
            if (param.Content != signInClientStore.Id)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)UpdaterMessengerIds.CheckForward,
                    Payload = string.IsNullOrWhiteSpace(param.Content) ? Helper.EmptyArray : serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(param.Content) || param.Content == signInClientStore.Id)
            {
                updaterTransfer.Check();
            }
        }
    }
}
