using linker.libs.api;
using linker.plugins.tuntap.messenger;
using MemoryPack;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.config;
using linker.tunnel.connection;
using linker.plugins.tuntap.proxy;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.tuntap.config;
using linker.libs;
using System.Net;
using linker.client.config;

namespace linker.plugins.tuntap
{
    public sealed class TuntapClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;

        public TuntapClientApiController(MessengerSender messengerSender, TuntapTransfer tuntapTransfer, ClientSignInState clientSignInState, FileConfig config, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.tuntapTransfer = tuntapTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;
        }

        public RouteItemListInfo RouteItems(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tuntapTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new RouteItemListInfo
                {
                    List = tuntapTransfer.RouteItems.Select(c =>
                    {
                        uint maskValue = NetworkHelper.GetPrefixIP(c.PrefixLength);
                        IPAddress mask = NetworkHelper.GetPrefixIp(maskValue);
                        IPAddress _ip = NetworkHelper.ToNetworkIp(c.Address, maskValue);
                        return new
                        {
                            IP = c.Address,
                            Network = _ip,
                            PrefixLength = c.PrefixLength,
                            PrefixIP = mask,
                        };
                    }).ToArray(),
                    HashCode = version,
                    Running = tuntapTransfer.Status == TuntapStatus.Running,
                    IP = runningConfig.Data.Tuntap.IP,
                };
            }
            return new RouteItemListInfo { HashCode = version };
        }


        public ConnectionListInfo Connections(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tuntapProxy.Version.Eq(hashCode, out ulong version) == false)
            {
                return new ConnectionListInfo
                {
                    List = tuntapProxy.GetConnections(),
                    HashCode = version
                };
            }
            return new ConnectionListInfo { HashCode = version };
        }
        public void RemoveConnection(ApiControllerParamsInfo param)
        {
            tuntapProxy.RemoveConnection(param.Content);
        }

        /// <summary>
        /// 获取所有客户端的网卡信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public TuntabListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (tuntapTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new TuntabListInfo
                {
                    List = tuntapTransfer.Infos,
                    HashCode = version
                };
            }
            return new TuntabListInfo { HashCode = version };
        }
        /// <summary>
        /// 刷新网卡信息
        /// </summary>
        /// <param name="param"></param>
        public void Refresh(ApiControllerParamsInfo param)
        {
            tuntapTransfer.RefreshConfig();
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Run(ApiControllerParamsInfo param)
        {
            //运行自己的
            if (param.Content == config.Data.Client.Id)
            {
                tuntapTransfer.Shutdown();
                tuntapTransfer.Setup();
            }
            else
            {
                //运行别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.RunForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                }).ConfigureAwait(false);
            }
            return true;
        }
        /// <summary>
        /// 停止网卡
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Stop(ApiControllerParamsInfo param)
        {
            //停止自己的
            if (param.Content == config.Data.Client.Id)
            {
                tuntapTransfer.Shutdown();
            }
            else
            {
                //停止别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.StopForward,
                    Payload = MemoryPackSerializer.Serialize(param.Content)
                }).ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// 更新网卡信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Update(ApiControllerParamsInfo param)
        {

            TuntapInfo info = param.Content.DeJson<TuntapInfo>();
            //更新自己的
            if (info.MachineId == config.Data.Client.Id)
            {
                tuntapTransfer.UpdateConfig(info);
            }
            else
            {
                //更新别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.UpdateForward,
                    Payload = MemoryPackSerializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }


        public sealed class RouteItemListInfo
        {
            public object[] List { get; set; }
            public IPAddress IP { get; set; }
            public bool Running { get; set; }
            public ulong HashCode { get; set; }
        }
        public sealed class TuntabListInfo
        {
            public ConcurrentDictionary<string, TuntapInfo> List { get; set; }
            public ulong HashCode { get; set; }
        }
        public sealed class ConnectionListInfo
        {
            public ConcurrentDictionary<string, ITunnelConnection> List { get; set; }
            public ulong HashCode { get; set; }
        }
    }
}
