using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.upnp;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace linker.messenger.tunnel.client
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

        public async Task<List<PortMappingInfo>> GetMapping(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id || string.IsNullOrWhiteSpace(param.Content))
            {
                return tunnelNetworkTransfer.GetMapping();
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.UpnpGetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<List<PortMappingInfo>>(resp.Data.Span);
            }
            return [];
        }
        public async Task<List<PortMappingInfo>> GetMappingLocal(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id || string.IsNullOrWhiteSpace(param.Content))
            {
                return tunnelNetworkTransfer.GetMappingLocal();
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.UpnpGetLocalForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                return serializer.Deserialize<List<PortMappingInfo>>(resp.Data.Span);
            }
            return [];
        }
        public async Task<bool> AddMapping(ApiControllerParamsInfo param)
        {
            KeyValueInfo<string, PortMappingInfo> info = param.Content.DeJson<KeyValueInfo<string, PortMappingInfo>>();
            if (info.Key == signInClientStore.Id || string.IsNullOrWhiteSpace(info.Key))
            {
                await tunnelNetworkTransfer.AddMapping(info.Value).ConfigureAwait(false);
                return true;
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.UpnpAddForward,
                Payload = serializer.Serialize(new KeyValuePair<string, PortMappingInfo>(info.Key, info.Value))
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> DelMapping(ApiControllerParamsInfo param)
        {
            KeyValueInfo<string, KeyValueInfo<int, ProtocolType>> info = param.Content.DeJson<KeyValueInfo<string, KeyValueInfo<int, ProtocolType>>>();
            if (info.Key == signInClientStore.Id || string.IsNullOrWhiteSpace(info.Key))
            {
                await tunnelNetworkTransfer.DelMapping(info.Value.Key, info.Value.Value).ConfigureAwait(false);
                return true;
            }

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)TunnelMessengerIds.UpnpDelForward,
                Payload = serializer.Serialize(new KeyValuePair<string, KeyValuePair<int, ProtocolType>>(info.Key, new KeyValuePair<int, ProtocolType>(info.Value.Key, info.Value.Value)))
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
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
        public TunnelOperatingInfo Operating(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tunnelTransfer.OperatingVersion.Eq(hashCode, out ulong version) == false)
            {
                return new TunnelOperatingInfo
                {
                    List = tunnelTransfer.Operating,
                    HashCode = version
                };
            }
            return new TunnelOperatingInfo { HashCode = version };
        }
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool Connect(ApiControllerParamsInfo param)
        {
            TunnelConnectInfo tunnelConnectInfo = param.Content.DeJson<TunnelConnectInfo>();
            _ = tunnelTransfer.ConnectAsync(tunnelConnectInfo.ToMachineId, tunnelConnectInfo.TransactionId, tunnelConnectInfo.DenyProtocols, flag: "hand", exTunnelTypes: [TunnelType.Relay]);

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
                await tunnelClientStore.SetInIp(tunnelSetRouteLevelInfo.InIp).ConfigureAwait(false);
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
                return await tunnelMessengerAdapter.GetTunnelTransports(Helper.GlobalString).ConfigureAwait(false);
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
                await tunnelMessengerAdapter.SetTunnelTransports(Helper.GlobalString, info.Data).ConfigureAwait(false);
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

        public sealed class TunnelOperatingInfo
        {
            public ConcurrentDictionary<string, bool> List { get; set; }
            public ulong HashCode { get; set; }
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
