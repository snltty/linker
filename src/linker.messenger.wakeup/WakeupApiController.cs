using linker.libs;
using linker.messenger.signin;
using linker.messenger.api;
using linker.libs.extends;
using linker.libs.web;

namespace linker.messenger.wakeup
{
    public sealed class WakeupApiController : IApiController
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly IAccessStore accessStore;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly WakeupTransfer wakeupTransfer;
        public WakeupApiController(IMessengerSender messengerSender, SignInClientState signInClientState, IAccessStore accessStore,
            ISignInClientStore signInClientStore, ISerializer serializer, WakeupTransfer wakeupTransfer)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.accessStore = accessStore;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.wakeupTransfer = wakeupTransfer;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<WakeupInfo>> Get(ApiControllerParamsInfo param)
        {
            WakeupSearchForwardInfo info = param.Content.DeJson<WakeupSearchForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.WakeupSelf) == false) return new List<WakeupInfo>();
                return wakeupTransfer.Get(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.WakeupOther) == false) return new List<WakeupInfo>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WakeupMessengerIds.GetForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<WakeupInfo>>(resp.Data.Span);
            }
            return new List<WakeupInfo>();
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            WakeupAddForwardInfo info = param.Content.DeJson<WakeupAddForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.WakeupSelf) == false) return false;
                return wakeupTransfer.Add(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.WakeupOther) == false) return false;

            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WakeupMessengerIds.AddForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Remove(ApiControllerParamsInfo param)
        {
            WakeupRemoveForwardInfo info = param.Content.DeJson<WakeupRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.WakeupSelf) == false) return false;
                return wakeupTransfer.Remove(info.Id);
            }

            if (accessStore.HasAccess(AccessValue.WakeupOther) == false) return false;
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WakeupMessengerIds.RemoveForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<string[]> ComNames(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                return wakeupTransfer.ComNames();
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WakeupMessengerIds.ComsForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);

            if(resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string[]>(resp.Data.Span);
            }
            return Array.Empty<string>();
        }
        /// <summary>
        /// hid列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<string[]> HidIds(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                return wakeupTransfer.HidIds();
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WakeupMessengerIds.HidsForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<string[]>(resp.Data.Span);
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Send(ApiControllerParamsInfo param)
        {
            WakeupSendForwardInfo info = param.Content.DeJson<WakeupSendForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.WakeupSelf) == false) return false;
                _ = wakeupTransfer.Send(info.Data);
                return true;
            }

            if (accessStore.HasAccess(AccessValue.WakeupOther) == false) return false;
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)WakeupMessengerIds.SendForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
        }
    }
}
