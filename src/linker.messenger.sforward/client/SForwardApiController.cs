using linker.libs.extends;
using linker.plugins.sforward.messenger;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.libs;
using linker.messenger.api;
using linker.libs.web;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardApiController : IApiController
    {
        private readonly SForwardClientTransfer forwardTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly SForwardDecenter sForwardDecenter;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;
        private readonly SForwardPlanHandle sForwardPlanHandle;

        public SForwardApiController(SForwardClientTransfer forwardTransfer, IMessengerSender messengerSender, SignInClientState signInClientState, ISignInClientStore signInClientStore, SForwardDecenter sForwardDecenter, ISForwardClientStore sForwardClientStore, ISerializer serializer, IAccessStore accessStore, SForwardPlanHandle sForwardPlanHandle)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.sForwardDecenter = sForwardDecenter;
            this.sForwardClientStore = sForwardClientStore;
            this.serializer = serializer;
            this.accessStore = accessStore;
            this.sForwardPlanHandle = sForwardPlanHandle;
        }

        public void Refresh(ApiControllerParamsInfo param)
        {
            sForwardDecenter.Refresh();
        }
        /// <summary>
        /// 获取数量
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public SForwardListInfo GetCount(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (sForwardDecenter.DataVersion.Eq(hashCode, out ulong version) == false)
            {
                return new SForwardListInfo
                {
                    List = sForwardDecenter.CountDic,
                    HashCode = version
                };
            }
            return new SForwardListInfo { HashCode = version };
        }

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<SForwardInfo>> Get(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardShowSelf) == false) return new List<SForwardInfo>();
                return sForwardClientStore.Get().ToList();
            }

            if (accessStore.HasAccess(AccessValue.ForwardShowOther) == false) return new List<SForwardInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.GetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<SForwardInfo>>(resp.Data.Span);
            }
            return new List<SForwardInfo>();
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            SForwardAddForwardInfo info = param.Content.DeJson<SForwardAddForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Add(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.AddClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Remove(ApiControllerParamsInfo param)
        {
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Remove(info.Id);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.RemoveClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Start(ApiControllerParamsInfo param)
        {
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                forwardTransfer.Start(info.Id);
                return true;
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.StartClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return true;
        }
        public async Task<bool> Stop(ApiControllerParamsInfo param)
        {
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                forwardTransfer.Stop(info.Id);
                return true;
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.StopClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// 测试服务
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> TestLocal(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                forwardTransfer.SubscribeTest();
                return true;
            }
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.TestClientForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return true;
        }

    }

    public sealed class SForwardListInfo
    {
        public ConcurrentDictionary<string, int> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
