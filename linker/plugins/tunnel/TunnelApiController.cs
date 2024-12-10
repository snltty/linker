using linker.config;
using linker.plugins.tunnel.messenger;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs.api;
using linker.libs.extends;
using MemoryPack;
using System.Collections.Concurrent;
using linker.tunnel.wanport;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.tunnel.excludeip;
using linker.plugins.access;

namespace linker.plugins.tunnel
{
    /// <summary>
    /// 管理接口
    /// </summary>
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly FileConfig config;
        private readonly TunnelWanPortTransfer compactTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly IMessengerSender messengerSender;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        private readonly ITunnelAdapter tunnelMessengerAdapter;
        private readonly TunnelExcludeIPTransfer excludeIPTransfer;
        private readonly AccessTransfer accessTransfer;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public TunnelApiController(FileConfig config, TunnelWanPortTransfer compactTransfer, ClientSignInState clientSignInState, IMessengerSender messengerSender, TunnelConfigTransfer tunnelConfigTransfer, ITunnelAdapter tunnelMessengerAdapter, TunnelExcludeIPTransfer excludeIPTransfer, AccessTransfer accessTransfer, ClientConfigTransfer clientConfigTransfer)
        {
            this.config = config;
            this.compactTransfer = compactTransfer;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.tunnelConfigTransfer = tunnelConfigTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
            this.excludeIPTransfer = excludeIPTransfer;
            this.accessTransfer = accessTransfer;
            this.clientConfigTransfer = clientConfigTransfer;
        }

        /// <summary>
        /// 获取所有人的隧道信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public TunnelListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tunnelConfigTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new TunnelListInfo
                {
                    List = tunnelConfigTransfer.Config,
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
            tunnelConfigTransfer.RefreshConfig();
        }

        /// <summary>
        /// 设置网关层级
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> SetRouteLevel(ApiControllerParamsInfo param)
        {
            TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo = param.Content.DeJson<TunnelTransportRouteLevelInfo>();

            if (tunnelTransportFileConfigInfo.MachineId == clientConfigTransfer.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TunnelChangeSelf) == false) return false;
                tunnelConfigTransfer.OnLocalRouteLevel(tunnelTransportFileConfigInfo);
            }
            else
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TunnelChangeOther) == false) return false;

                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TunnelMessengerIds.RouteLevelForward,
                    Payload = MemoryPackSerializer.Serialize(tunnelTransportFileConfigInfo)
                }).ConfigureAwait(false);
            }

            return true;
        }
        /// <summary>
        /// 获取打洞协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<TunnelTransportItemInfo> GetTransports(ApiControllerParamsInfo param)
        {
            return tunnelMessengerAdapter.GetTunnelTransports();
        }
        /// <summary>
        /// 设置打洞协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Transport)]
        public bool SetTransports(ApiControllerParamsInfo param)
        {
            List<TunnelTransportItemInfo> info = param.Content.DeJson<List<TunnelTransportItemInfo>>();
            tunnelMessengerAdapter.SetTunnelTransports(info, true);
            return true;
        }

        public sealed class TunnelListInfo
        {
            public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> List { get; set; }
            public ulong HashCode { get; set; }
        }

    }

}
