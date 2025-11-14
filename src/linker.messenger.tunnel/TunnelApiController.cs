using linker.tunnel.transport;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.libs;
using linker.messenger.api;
using linker.tunnel.connection;
using linker.tunnel;
using linker.libs.web;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 管理接口
    /// </summary>
    public sealed class TunnelApiController : IApiController
    {
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISignInClientStore signInClientStore;
        private readonly TunnelDecenter tunnelDecenter;
        private readonly ITunnelClientStore tunnelClientStore;
        private readonly ISerializer serializer;
        private readonly TunnelNetworkTransfer tunnelNetworkTransfer;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;

        public TunnelApiController(SignInClientState signInClientState, IMessengerSender messengerSender, ISignInClientStore signInClientStore,
            TunnelDecenter tunnelDecenter, ITunnelClientStore tunnelClientStore, ISerializer serializer, TunnelNetworkTransfer tunnelNetworkTransfer,
            TunnelTransfer tunnelTransfer, ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.signInClientStore = signInClientStore;
            this.tunnelDecenter = tunnelDecenter;
            this.tunnelClientStore = tunnelClientStore;
            this.serializer = serializer;
            this.tunnelNetworkTransfer = tunnelNetworkTransfer;
            this.tunnelTransfer = tunnelTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        /// <summary>
        /// 获取所有人的隧道信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public TunnelListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tunnelDecenter.DataVersion.Eq(hashCode, out ulong version) == false)
            {
                return new TunnelListInfo
                {
                    List = tunnelDecenter.Config,
                    HashCode = version
                };
            }
            return new TunnelListInfo { HashCode = version };
        }
        /// <summary>
        /// 刷新隧道信息
        /// </summary>
        /// <param name="param"></param>
        public void Refresh(ApiControllerParamsInfo param)
        {
            tunnelDecenter.Refresh();
        }

        /// <summary>
        /// 正在操作列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, bool> Operating(ApiControllerParamsInfo param)
        {
            return tunnelTransfer.Operating;
        }
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Connect(ApiControllerParamsInfo param)
        {
            TunnelConnectInfo tunnelConnectInfo = param.Content.DeJson<TunnelConnectInfo>();
            _ = tunnelTransfer.ConnectAsync(tunnelConnectInfo.ToMachineId, tunnelConnectInfo.TransactionId, tunnelConnectInfo.DenyProtocols);

            return true;
        }

        /// <summary>
        /// 设置网关层级
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> SetRouteLevel(ApiControllerParamsInfo param)
        {
            TunnelSetRouteLevelInfo tunnelSetRouteLevelInfo = param.Content.DeJson<TunnelSetRouteLevelInfo>();

            if (tunnelSetRouteLevelInfo.MachineId == signInClientStore.Id)
            {
                await tunnelClientStore.SetRouteLevelPlus(tunnelSetRouteLevelInfo.RouteLevelPlus).ConfigureAwait(false);
                await tunnelClientStore.SetPortMap(tunnelSetRouteLevelInfo.PortMapLan, tunnelSetRouteLevelInfo.PortMapWan).ConfigureAwait(false);
            }
            else
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.RouteLevelForward,
                    Payload = serializer.Serialize(tunnelSetRouteLevelInfo)
                }).ConfigureAwait(false);
            }

            return true;
        }
        /// <summary>
        /// 获取打洞协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<TunnelTransportItemInfo>> GetTransports(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id || string.IsNullOrWhiteSpace(param.Content))
            {
                return await tunnelMessengerAdapter.GetTunnelTransports("default").ConfigureAwait(false);
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.TransportGetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<List<TunnelTransportItemInfo>>(resp.Data.Span);
            }

            return [];

        }
        /// <summary>
        /// 设置打洞协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [Access(AccessValue.Transport)]
        public async Task<bool> SetTransports(ApiControllerParamsInfo param)
        {
            TunnelTransportItemSetInfo info = param.Content.DeJson<TunnelTransportItemSetInfo>();
            if (info.MachineId == signInClientStore.Id || string.IsNullOrWhiteSpace(info.MachineId))
            {
                await tunnelMessengerAdapter.SetTunnelTransports("default", info.Data).ConfigureAwait(false);
                return true;
            }
            await tunnelMessengerAdapter.SetTunnelTransports(info.MachineId, info.Data).ConfigureAwait(false);
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.TransportSetForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<TunnelLocalNetworkInfo> GetNetwork(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                return tunnelNetworkTransfer.GetLocalNetwork();
            }
            else
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.NetworkForward,
                    Payload = serializer.Serialize(param.Content)
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
                {
                    return serializer.Deserialize<TunnelLocalNetworkInfo>(resp.Data.Span);
                }
            }
            return new TunnelLocalNetworkInfo();
        }

        public sealed class TunnelListInfo
        {
            public ConcurrentDictionary<string, TunnelRouteLevelInfo> List { get; set; }
            public ulong HashCode { get; set; }
        }
        public sealed class TunnelConnectInfo
        {
            public string ToMachineId { get; set; }
            public string TransactionId { get; set; }
            public TunnelProtocolType DenyProtocols { get; set; }
        }

    }

}
