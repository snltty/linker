using linker.libs.extends;
using System.Net;
using linker.libs;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.messenger.forward.proxy;
using linker.messenger.api;
using linker.libs.web;

namespace linker.messenger.forward
{
    public sealed class ForwardApiController : IApiController
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly ForwardProxy forwardProxy;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly IAccessStore accessStore;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;

        public ForwardApiController(ForwardTransfer forwardTransfer, ForwardProxy forwardProxy, IMessengerSender messengerSender, SignInClientState signInClientState, IAccessStore accessStore, ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.forwardTransfer = forwardTransfer;
            this.forwardProxy = forwardProxy;
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.accessStore = accessStore;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }

        /// <summary>
        /// 获取隧道连接
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public ConnectionListInfo Connections(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (forwardProxy.Version.Eq(hashCode, out ulong version) == false)
            {
                return new ConnectionListInfo
                {
                    List = forwardProxy.GetConnections(),
                    HashCode = version
                };
            }
            return new ConnectionListInfo { HashCode = version };
        }

        /// <summary>
        /// 移除隧道连接
        /// </summary>
        /// <param name="param"></param>
        [Access(AccessValue.TunnelRemove)]
        public void RemoveConnection(ApiControllerParamsInfo param)
        {
            forwardProxy.RemoveConnection(param.Content);
        }

        /// <summary>
        /// 获取绑定IP列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public IPAddress[] BindIPs(ApiControllerParamsInfo param)
        {
            return NetworkHelper.GetIPV4();
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<ForwardInfo>> Get(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardShowSelf) == false) return new List<ForwardInfo>();
                return forwardTransfer.Get().ToList();
            }
            if (accessStore.HasAccess(AccessValue.ForwardShowOther) == false) return new List<ForwardInfo>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ForwardMessengerIds.GetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<ForwardInfo>>(resp.Data.Span);
            }
            return new List<ForwardInfo>();
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            ForwardAddForwardInfo info = param.Content.DeJson<ForwardAddForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Add(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;

            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ForwardMessengerIds.AddForward,
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
            ForwardRemoveForwardInfo info = param.Content.DeJson<ForwardRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Remove(info.Id);
            }

            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ForwardMessengerIds.RemoveForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 检测
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Test(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                forwardTransfer.SubscribeTest();
                return true;
            }
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ForwardMessengerIds.SubTestForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return true;
        }
    }

    public sealed class ForwardListInfo
    {
        public ConcurrentDictionary<string, int> List { get; set; }
        public ulong HashCode { get; set; }
    }
    public sealed class ConnectionListInfo
    {
        public ConcurrentDictionary<string, ITunnelConnection> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
