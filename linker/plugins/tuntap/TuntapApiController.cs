using linker.libs.api;
using linker.plugins.tuntap.messenger;
using linker.serializer;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.config;
using linker.tunnel.connection;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.plugins.tuntap.config;
using linker.client.config;
using linker.plugins.tuntap.lease;
using System.Net;
using linker.libs;
using linker.plugins.access;
using linker.messenger;
using linker.messenger.signin;

namespace linker.plugins.tuntap
{
    public sealed class TuntapClientApiController : IApiClientController
    {
        private readonly IMessengerSender messengerSender;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly SignInClientState signInClientState;
        private readonly TuntapProxy tuntapProxy;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly LeaseClientTreansfer leaseClientTreansfer;
        private readonly TuntapPingTransfer pingTransfer;
        private readonly AccessTransfer accessTransfer;
        private readonly ISignInClientStore signInClientStore;
        private readonly TuntapDecenter tuntapDecenter;
        private readonly TuntapAdapter tuntapAdapter;

        public TuntapClientApiController(IMessengerSender messengerSender, TuntapTransfer tuntapTransfer, SignInClientState signInClientState,
            TuntapProxy tuntapProxy,TuntapConfigTransfer tuntapConfigTransfer, LeaseClientTreansfer leaseClientTreansfer, 
            TuntapPingTransfer pingTransfer, AccessTransfer accessTransfer, ISignInClientStore signInClientStore, TuntapDecenter tuntapDecenter, TuntapAdapter tuntapAdapter)
        {
            this.messengerSender = messengerSender;
            this.tuntapTransfer = tuntapTransfer;
            this.signInClientState = signInClientState;
            this.tuntapProxy = tuntapProxy;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.leaseClientTreansfer = leaseClientTreansfer;
            this.pingTransfer = pingTransfer;
            this.accessTransfer = accessTransfer;
            this.signInClientStore = signInClientStore;
            this.tuntapDecenter = tuntapDecenter;
            this.tuntapAdapter = tuntapAdapter;
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

        [ClientApiAccess(ClientApiAccess.TunnelRemove)]
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
            if (tuntapDecenter.DataVersion.Eq(hashCode, out ulong version) == false)
            {
                return new TuntabListInfo
                {
                    List = tuntapDecenter.Infos,
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
            tuntapDecenter.Refresh();
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> Run(ApiControllerParamsInfo param)
        {
            //运行自己的
            if (param.Content == signInClientStore.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TuntapStatusSelf) == false) return false;

                await tuntapAdapter.RetstartDevice();
            }
            else
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TuntapStatusOther) == false) return false;
                //运行别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.RunForward,
                    Payload = Serializer.Serialize(param.Content)
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
            if (param.Content == signInClientStore.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TuntapStatusSelf) == false) return false;
                tuntapAdapter.StopDevice();
            }
            else
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TuntapStatusOther) == false) return false;
                //停止别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.StopForward,
                    Payload = Serializer.Serialize(param.Content)
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
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TuntapChangeSelf) == false) return false;
                tuntapConfigTransfer.Update(info);
            }
            else
            {
                if (accessTransfer.HasAccess(ClientApiAccess.TuntapChangeOther) == false) return false;
                //更新别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.UpdateForward,
                    Payload = Serializer.Serialize(info)
                }).ConfigureAwait(false);
            }
            return true;
        }


        /// <summary>
        /// 计算网络
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public NetworkInfo CalcNetwork(ApiControllerParamsInfo param)
        {
            NetworkParamInfo info = param.Content.DeJson<NetworkParamInfo>();

            uint ip = NetworkHelper.IP2Value(info.IP);
            uint prefixValue = NetworkHelper.PrefixLength2Value(info.PrefixLength);
            uint network = NetworkHelper.NetworkValue2Value(ip, prefixValue);
            uint broadcast = NetworkHelper.BroadcastValue2Value(ip, prefixValue);
            return new NetworkInfo
            {
                Network = NetworkHelper.Value2IP(network),
                Broadcast = NetworkHelper.Value2IP(broadcast),
                Gateway = NetworkHelper.ToGatewayIP(ip, prefixValue),
                Start = NetworkHelper.Value2IP(network + 2),
                End = NetworkHelper.Value2IP(broadcast - 1),
                Count = (int)(broadcast - network - 2),
            };
        }
        /// <summary>
        /// 添加网络
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [ClientApiAccess(ClientApiAccess.Lease)]
        public async Task AddNetwork(ApiControllerParamsInfo param)
        {
            await leaseClientTreansfer.AddNetwork(param.Content.DeJson<LeaseInfo>());
            await leaseClientTreansfer.LeaseChange();
            tuntapConfigTransfer.RefreshIP();
        }
        /// <summary>
        /// 获取网络
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<LeaseInfo> GetNetwork(ApiControllerParamsInfo param)
        {
            return await leaseClientTreansfer.GetNetwork();
        }

        /// <summary>
        /// 订阅ping
        /// </summary>
        /// <param name="param"></param>
        public void SubscribePing(ApiControllerParamsInfo param)
        {
            pingTransfer.SubscribePing();
        }
        /// <summary>
        /// 订阅转发连通测试
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<TuntapForwardTestWrapInfo> SubscribeForwardTest(ApiControllerParamsInfo param)
        {
            TuntapForwardTestWrapInfo tuntapForwardTestWrapInfo = param.Content.DeJson<TuntapForwardTestWrapInfo>();

            if (tuntapForwardTestWrapInfo.MachineId == signInClientStore.Id)
            {
                await pingTransfer.SubscribeForwardTest(tuntapForwardTestWrapInfo.List);
            }
            else
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.SubscribeForwardTestForward,
                    Payload = Serializer.Serialize(tuntapForwardTestWrapInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.Length > 0)
                {
                    tuntapForwardTestWrapInfo = Serializer.Deserialize<TuntapForwardTestWrapInfo>(resp.Data.Span);
                }
            }
            return tuntapForwardTestWrapInfo;
        }
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

    public sealed class NetworkParamInfo
    {
        public IPAddress IP { get; set; }
        public byte PrefixLength { get; set; }
    }
    public sealed class NetworkInfo
    {
        public IPAddress Network { get; set; }
        public IPAddress Gateway { get; set; }
        public IPAddress Start { get; set; }
        public IPAddress End { get; set; }
        public IPAddress Broadcast { get; set; }
        public int Count { get; set; }
    }
}
