using linker.libs;
using linker.messenger.node;
using linker.messenger.relay.messenger;
using linker.tunnel.connection;
using linker.tunnel.transport;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继节点操作
    /// </summary>
    public class RelayServerNodeTransfer : NodeTransfer<RelayServerConfigInfo, RelayServerNodeStoreInfo, RelayServerNodeReportInfo>
    {
        private readonly ISerializer serializer;
        private readonly IMessengerSender messengerSender;

        private readonly RelayServerConnectionTransfer relayServerConnectionTransfer;
        private readonly RelayServerNodeReportTransfer relayServerNodeReportTransfer;

        public RelayServerNodeTransfer(ISerializer serializer, IMessengerSender messengerSender, RelayServerConnectionTransfer relayServerConnectionTransfer,
            ICommonStore commonStore, IRelayNodeConfigStore nodeConfigStore,
            RelayServerNodeReportTransfer  relayServerNodeReportTransfer)
            : base(commonStore, nodeConfigStore, relayServerNodeReportTransfer)
        {
            this.serializer = serializer;
            this.messengerSender = messengerSender;
            this.relayServerConnectionTransfer = relayServerConnectionTransfer;
            this.relayServerNodeReportTransfer = relayServerNodeReportTransfer;
        }

        public async Task<RelayCacheInfo> TryGetRelayCache(RelayMessageInfo relayMessage)
        {
            try
            {
                if (relayServerConnectionTransfer.TryGet(ConnectionSideType.Master, relayMessage.MasterId, out ConnectionInfo connection) == false)
                {
                    return null;
                }

                //ask 是发起端来的，那key就是 发起端->目标端， answer的，目标和来源会交换，所以转换一下
                string key = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}->{relayMessage.FlowId}" : $"{relayMessage.ToId}->{relayMessage.FromId}->{relayMessage.FlowId}";
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection.Connection,
                    MessengerId = (ushort)RelayMessengerIds.GetCache,
                    Payload = serializer.Serialize(new ValueTuple<string, string>(key, Config.NodeId)),
                    Timeout = 1000
                }).ConfigureAwait(false);
                if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
                {
                    RelayCacheInfo result = serializer.Deserialize<RelayCacheInfo>(resp.Data.Span);
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{ex}");
            }
            return null;
        }

        public bool Validate(TunnelProtocolType tunnelProtocolType)
        {
            return (Config.Protocol & tunnelProtocolType) == tunnelProtocolType;
        }
        /// <summary>
        /// 无效请求
        /// </summary>
        /// <returns></returns>
        public bool Validate(RelayCacheInfo relayCache)
        {
            return ValidateConnection(relayCache) && ValidateBytes(relayCache);
        }
        /// <summary>
        /// 连接数是否够
        /// </summary>
        /// <returns></returns>
        private bool ValidateConnection(RelayCacheInfo relayCache)
        {
            bool res = Config.Connections == 0 || Config.Connections * 2 > relayServerNodeReportTransfer.ConnectionNum;
            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  validate connection false,{relayServerNodeReportTransfer.ConnectionNum}/{Config.Connections * 2}");

            return res;
        }
        /// <summary>
        /// 流量是否够
        /// </summary>
        /// <returns></returns>
        private bool ValidateBytes(RelayCacheInfo relayCache)
        {
            bool res = Config.DataEachMonth == 0
                || (Config.DataEachMonth > 0 && Config.DataRemain > 0);

            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateBytes false,{Config.DataRemain}bytes/{Config.DataEachMonth}gb");

            return res;
        }

    }
}
