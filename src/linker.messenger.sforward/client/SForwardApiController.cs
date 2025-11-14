using linker.libs.extends;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.libs;
using linker.messenger.api;
using linker.libs.web;
using linker.messenger.sforward.server;
using linker.messenger.sforward.messenger;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardApiController : IApiController
    {
        private readonly SForwardClientTransfer forwardTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISForwardClientStore sForwardClientStore;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;
        private readonly SForwardClientTestTransfer sForwardClientTestTransfer;

        public SForwardApiController(SForwardClientTransfer forwardTransfer, IMessengerSender messengerSender,
            SignInClientState signInClientState, ISignInClientStore signInClientStore,
            ISForwardClientStore sForwardClientStore, ISerializer serializer, IAccessStore accessStore,
            SForwardClientTestTransfer sForwardClientTestTransfer)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.sForwardClientStore = sForwardClientStore;
            this.serializer = serializer;
            this.accessStore = accessStore;
            this.sForwardClientTestTransfer = sForwardClientTestTransfer;
        }

        public List<SForwardServerNodeReportInfo> Subscribe(ApiControllerParamsInfo param)
        {
            sForwardClientTestTransfer.Subscribe();
            return sForwardClientTestTransfer.Nodes;
        }

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<SForwardInfo191>> Get(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardShowSelf) == false) return new List<SForwardInfo191>();
                return sForwardClientStore.Get().ToList();
            }

            if (accessStore.HasAccess(AccessValue.ForwardShowOther) == false) return new List<SForwardInfo191>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.GetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<SForwardInfo191>>(resp.Data.Span);
            }
            return new List<SForwardInfo191>();
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            SForwardAddForwardInfo191 info = param.Content.DeJson<SForwardAddForwardInfo191>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Add(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.AddClientForward191,
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



        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Edit(ApiControllerParamsInfo param)
        {
            SForwardServerNodeUpdateInfo info = param.Content.DeJson<SForwardServerNodeUpdateInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.EditForward,
                Payload = serializer.Serialize(new SForwardServerNodeUpdateWrapInfo
                {
                    Info = info,
                })
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 重启节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.ExitForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            UpdateInfo info = param.Content.DeJson<UpdateInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.UpdateForward,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(info.Key, info.Value))
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

    }

    public sealed class UpdateInfo
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public sealed class SForwardListInfo
    {
        public ConcurrentDictionary<string, int> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
