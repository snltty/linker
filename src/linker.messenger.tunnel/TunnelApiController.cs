using linker.tunnel.transport;
using linker.libs.api;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.plugins.tunnel;
using linker.libs;
using linker.messenger.api;

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

        public TunnelApiController(SignInClientState signInClientState, IMessengerSender messengerSender, ISignInClientStore signInClientStore, TunnelDecenter tunnelDecenter, ITunnelClientStore tunnelClientStore, ISerializer serializer)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.signInClientStore = signInClientStore;
            this.tunnelDecenter = tunnelDecenter;
            this.tunnelClientStore = tunnelClientStore;
            this.serializer = serializer;
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
        /// 设置网关层级
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> SetRouteLevel(ApiControllerParamsInfo param)
        {
            TunnelSetRouteLevelInfo tunnelSetRouteLevelInfo = param.Content.DeJson<TunnelSetRouteLevelInfo>();

            if (tunnelSetRouteLevelInfo.MachineId == signInClientStore.Id)
            {
                await tunnelClientStore.SetRouteLevelPlus(tunnelSetRouteLevelInfo.RouteLevelPlus);
                await tunnelClientStore.SetPortMap(tunnelSetRouteLevelInfo.PortMapLan, tunnelSetRouteLevelInfo.PortMapWan);
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
            return await tunnelClientStore.GetTunnelTransports();
        }
        /// <summary>
        /// 设置打洞协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [Access(AccessValue.Transport)]
        public async Task<bool> SetTransports(ApiControllerParamsInfo param)
        {
            List<TunnelTransportItemInfo> info = param.Content.DeJson<List<TunnelTransportItemInfo>>();
            await tunnelClientStore.SetTunnelTransports(info);
            return true;
        }

        public sealed class TunnelListInfo
        {
            public ConcurrentDictionary<string, TunnelRouteLevelInfo> List { get; set; }
            public ulong HashCode { get; set; }
        }
    }

}
