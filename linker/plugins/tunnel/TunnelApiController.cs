using linker.config;
using linker.plugins.tunnel.messenger;
using linker.tunnel.adapter;
using linker.tunnel.transport;
using linker.libs.api;
using linker.libs.extends;
using MemoryPack;
using System.Collections.Concurrent;
using linker.tunnel.wanport;
using linker.client.config;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.tunnel.excludeip;
using System.Net;
using linker.libs;

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
        private readonly MessengerSender messengerSender;
        private readonly TunnelConfigTransfer tunnelConfigTransfer;
        private readonly ITunnelAdapter tunnelMessengerAdapter;
        private readonly TunnelExcludeIPTransfer excludeIPTransfer;

        public TunnelApiController(FileConfig config, TunnelWanPortTransfer compactTransfer, ClientSignInState clientSignInState, MessengerSender messengerSender, TunnelConfigTransfer tunnelConfigTransfer, ITunnelAdapter tunnelMessengerAdapter, TunnelExcludeIPTransfer excludeIPTransfer)
        {
            this.config = config;
            this.compactTransfer = compactTransfer;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.tunnelConfigTransfer = tunnelConfigTransfer;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
            this.excludeIPTransfer = excludeIPTransfer;
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
        /// 获取所有外网端口协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<TunnelWanPortTypeInfo> GetTypes(ApiControllerParamsInfo param)
        {
            return compactTransfer.GetTypes();
        }
        /// <summary>
        /// 设置外网端口协议
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool SetServers(ApiControllerParamsInfo param)
        {
            List<TunnelWanPortInfo> info = param.Content.DeJson<List<TunnelWanPortInfo>>();
            tunnelMessengerAdapter.SetTunnelWanPortProtocols(info, true);
            return true;
        }
        /// <summary>
        /// 设置网关层级
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> SetRouteLevel(ApiControllerParamsInfo param)
        {
            TunnelTransportRouteLevelInfo tunnelTransportFileConfigInfo = param.Content.DeJson<TunnelTransportRouteLevelInfo>();

            if (tunnelTransportFileConfigInfo.MachineId == config.Data.Client.Id)
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.TunnelChangeSelf) == false) return false;
                tunnelConfigTransfer.OnLocalRouteLevel(tunnelTransportFileConfigInfo);
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.TunnelChangeOther) == false) return false;

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
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool SetTransports(ApiControllerParamsInfo param)
        {
            List<TunnelTransportItemInfo> info = param.Content.DeJson<List<TunnelTransportItemInfo>>();
            tunnelMessengerAdapter.SetTunnelTransports(info, true);
            return true;
        }

        public ExcludeIPItem[] GetExcludeIPs(ApiControllerParamsInfo param)
        {
            return excludeIPTransfer.GetExcludeIPs();
        }

        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public void SetExcludeIPs(ApiControllerParamsInfo param)
        {
            ExcludeIPItem[] info = param.Content.DeJson<ExcludeIPItem[]>();
            excludeIPTransfer.SettExcludeIPs(info);
        }
        public sealed class TunnelListInfo
        {
            public ConcurrentDictionary<string, TunnelTransportRouteLevelInfo> List { get; set; }
            public ulong HashCode { get; set; }
        }

        /// <summary>
        /// 获取网卡接口列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public IPAddress[] GeInterfaces(ApiControllerParamsInfo param)
        {
            return NetworkHelper.GetIPV4();
        }
        /// <summary>
        /// 设置网卡接口
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccessAttribute(ClientApiAccess.Config)]
        public bool SetInterface(ApiControllerParamsInfo param)
        {
            IPAddress ip = IPAddress.Parse(param.Content);

            tunnelConfigTransfer.SetInterface(ip);

            return true;
        }
    }

}
