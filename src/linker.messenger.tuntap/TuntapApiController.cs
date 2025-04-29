using linker.libs.api;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.tunnel.connection;
using System.Net;
using linker.libs;
using linker.messenger.signin;
using linker.messenger.tuntap.lease;
using linker.messenger.api;
using linker.messenger.tuntap.messenger;

namespace linker.messenger.tuntap
{
    public sealed class TuntapApiController : IApiController
    {
        private readonly IMessengerSender messengerSender;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly SignInClientState signInClientState;
        private readonly TuntapProxy tuntapProxy;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly LeaseClientTreansfer leaseClientTreansfer;
        private readonly TuntapPingTransfer pingTransfer;
        private readonly IAccessStore accessStore;
        private readonly ISignInClientStore signInClientStore;
        private readonly TuntapDecenter tuntapDecenter;
        private readonly TuntapAdapter tuntapAdapter;
        private readonly ISerializer serializer;
        public TuntapApiController(IMessengerSender messengerSender, TuntapTransfer tuntapTransfer, SignInClientState signInClientState,
            TuntapProxy tuntapProxy, TuntapConfigTransfer tuntapConfigTransfer, LeaseClientTreansfer leaseClientTreansfer,
            TuntapPingTransfer pingTransfer, IAccessStore accessStore, ISignInClientStore signInClientStore, TuntapDecenter tuntapDecenter, TuntapAdapter tuntapAdapter, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.tuntapTransfer = tuntapTransfer;
            this.signInClientState = signInClientState;
            this.tuntapProxy = tuntapProxy;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.leaseClientTreansfer = leaseClientTreansfer;
            this.pingTransfer = pingTransfer;
            this.accessStore = accessStore;
            this.signInClientStore = signInClientStore;
            this.tuntapDecenter = tuntapDecenter;
            this.tuntapAdapter = tuntapAdapter;
            this.serializer = serializer;
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

        [Access(AccessValue.TunnelRemove)]
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
                if (accessStore.HasAccess(AccessValue.TuntapStatusSelf) == false) return false;

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning($"api restarting device");
                await tuntapAdapter.RetstartDevice().ConfigureAwait(false);
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.TuntapStatusOther) == false) return false;
                //运行别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.RunForward,
                    Payload = serializer.Serialize(param.Content)
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
                if (accessStore.HasAccess(AccessValue.TuntapStatusSelf) == false) return false;
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning($"api stop device");
                tuntapAdapter.StopDevice();
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.TuntapStatusOther) == false) return false;
                //停止别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.StopForward,
                    Payload = serializer.Serialize(param.Content)
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
                if (accessStore.HasAccess(AccessValue.TuntapChangeSelf) == false) return false;
                tuntapConfigTransfer.Update(info);
            }
            else
            {
                if (accessStore.HasAccess(AccessValue.TuntapChangeOther) == false) return false;
                //更新别人的
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.UpdateForward,
                    Payload = serializer.Serialize(info)
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

            uint ip = NetworkHelper.ToValue(info.IP);
            uint prefixValue = NetworkHelper.ToPrefixValue(info.PrefixLength);
            uint network = NetworkHelper.ToNetworkValue(ip, prefixValue);
            uint broadcast = NetworkHelper.ToBroadcastValue(ip, prefixValue);
            return new NetworkInfo
            {
                Network = NetworkHelper.ToIP(network),
                Broadcast = NetworkHelper.ToIP(broadcast),
                Gateway = NetworkHelper.ToGatewayIP(ip, prefixValue),
                Start = NetworkHelper.ToIP(network + 2),
                End = NetworkHelper.ToIP(broadcast - 1),
                Count = (int)(broadcast - network - 2),
            };
        }
        /// <summary>
        /// 添加网络
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [Access(AccessValue.Lease)]
        public async Task AddNetwork(ApiControllerParamsInfo param)
        {
            await leaseClientTreansfer.AddNetwork(param.Content.DeJson<LeaseInfo>()).ConfigureAwait(false);
            await leaseClientTreansfer.LeaseChange().ConfigureAwait(false);
            tuntapConfigTransfer.RefreshIP();
        }
        /// <summary>
        /// 获取网络
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<LeaseInfo> GetNetwork(ApiControllerParamsInfo param)
        {
            return await leaseClientTreansfer.GetNetwork().ConfigureAwait(false);
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
                await pingTransfer.SubscribeForwardTest(tuntapForwardTestWrapInfo.List).ConfigureAwait(false);
            }
            else
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.SubscribeForwardTestForward,
                    Payload = serializer.Serialize(tuntapForwardTestWrapInfo),
                    Timeout = 2000
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Span.Length > 0)
                {
                    tuntapForwardTestWrapInfo = serializer.Deserialize<TuntapForwardTestWrapInfo>(resp.Data.Span);
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
