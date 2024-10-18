using linker.libs.api;
using linker.plugins.tuntap.messenger;
using MemoryPack;
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

namespace linker.plugins.tuntap.client
{
    public sealed class TuntapClientApiController : IApiClientController
    {
        private readonly MessengerSender messengerSender;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly LeaseClientTreansfer leaseClientTreansfer;

        public TuntapClientApiController(MessengerSender messengerSender, TuntapTransfer tuntapTransfer, ClientSignInState clientSignInState, FileConfig config, TuntapProxy tuntapProxy, RunningConfig runningConfig, TuntapConfigTransfer tuntapConfigTransfer, LeaseClientTreansfer leaseClientTreansfer)
        {
            this.messengerSender = messengerSender;
            this.tuntapTransfer = tuntapTransfer;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.leaseClientTreansfer = leaseClientTreansfer;
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
            if (tuntapConfigTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new TuntabListInfo
                {
                    List = tuntapConfigTransfer.Infos,
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
            tuntapConfigTransfer.RefreshConfig();
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
                if (config.Data.Client.HasAccess(ClientApiAccess.TuntapStatusSelf) == false) return false;

                tuntapTransfer.Shutdown();
                await tuntapConfigTransfer.RefreshIPForce();
                tuntapTransfer.Setup();
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.TuntapStatusOther) == false) return false;
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
                if (config.Data.Client.HasAccess(ClientApiAccess.TuntapStatusSelf) == false) return false;
                tuntapTransfer.Shutdown();
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.TuntapStatusOther) == false) return false;
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
                if (config.Data.Client.HasAccess(ClientApiAccess.TuntapChangeSelf) == false) return false;
                tuntapConfigTransfer.UpdateConfig(info);
            }
            else
            {
                if (config.Data.Client.HasAccess(ClientApiAccess.TuntapChangeOther) == false) return false;
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

        public void SubscribePing(ApiControllerParamsInfo param)
        {
            tuntapConfigTransfer.SubscribePing();
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
