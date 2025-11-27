using linker.libs;
using linker.libs.timer;
using linker.messenger.relay.messenger;
using linker.tunnel.connection;
using linker.tunnel.transport;
using System.Collections.Concurrent;
using System.Net;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继节点操作
    /// </summary>
    public class RelayServerNodeTransfer
    {
        /// <summary>
        /// 配置了就用配置的，每配置就用一个默认的
        /// </summary>
        public RelayServerNodeInfo Node => relayServerNodeStore.Node;

        private uint connectionNum = 0;

        private long bytes = 0;
        private readonly RelaySpeedLimit limitTotal = new RelaySpeedLimit();
        private readonly ConcurrentDictionary<ulong, RelayTrafficCacheInfo> trafficDict = new();

        private readonly ISerializer serializer;
        private readonly IRelayServerNodeStore relayServerNodeStore;
        private readonly IMessengerSender messengerSender;
        private readonly RelayServerConnectionTransfer relayServerConnectionTransfer;

        public RelayServerNodeTransfer(ISerializer serializer, IRelayServerNodeStore relayServerNodeStore, IMessengerSender messengerSender, RelayServerConnectionTransfer relayServerConnectionTransfer)
        {
            this.serializer = serializer;
            this.relayServerNodeStore = relayServerNodeStore;
            this.messengerSender = messengerSender;
            this.relayServerConnectionTransfer = relayServerConnectionTransfer;

            limitTotal.SetLimit((uint)Math.Ceiling((Node.Bandwidth * 1024 * 1024) / 8.0));

            TrafficTask();
        }

        public async Task<RelayCacheInfo> TryGetRelayCache(RelayMessageInfo relayMessage)
        {
            try
            {
                if (relayServerConnectionTransfer.TryGet(relayMessage.MasterId, out IConnection connection) == false)
                {
                    return null;
                }

                //ask 是发起端来的，那key就是 发起端->目标端， answer的，目标和来源会交换，所以转换一下
                string key = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}->{relayMessage.FlowId}" : $"{relayMessage.ToId}->{relayMessage.FromId}->{relayMessage.FlowId}";
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)RelayMessengerIds.NodeGetCache,
                    Payload = serializer.Serialize(new ValueTuple<string, string>(key, Node.NodeId)),
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
            return true;
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
            bool res = Node.Connections == 0 || Node.Connections * 2 > connectionNum;
            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateConnection false,{connectionNum}/{Node.Connections * 2}");

            return res;
        }
        /// <summary>
        /// 流量是否够
        /// </summary>
        /// <returns></returns>
        private bool ValidateBytes(RelayCacheInfo relayCache)
        {
            bool res = Node.DataEachMonth == 0
                || (Node.DataEachMonth > 0 && Node.DataRemain > 0);

            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateBytes false,{Node.DataRemain}bytes/{Node.DataEachMonth}gb");

            return res;
        }

        /// <summary>
        /// 增加连接数
        /// </summary>
        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        /// <summary>
        /// 减少连接数
        /// </summary>
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }

        /// <summary>
        /// 是否需要总限速
        /// </summary>
        /// <returns></returns>
        public bool NeedLimit(RelayTrafficCacheInfo relayCache)
        {
            return limitTotal.NeedLimit();
        }
        /// <summary>
        /// 总限速
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool TryLimit(ref int length)
        {
            return limitTotal.TryLimit(ref length);
        }
        /// <summary>
        /// 总限速
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool TryLimitPacket(int length)
        {
            return limitTotal.TryLimitPacket(length);
        }


        /// <summary>
        /// 开始计算流量
        /// </summary>
        /// <param name="relayCache"></param>
        public void AddTrafficCache(RelayTrafficCacheInfo relayCache)
        {
            SetLimit(relayCache);
            trafficDict.TryAdd(relayCache.Cache.FlowId, relayCache);
        }
        /// <summary>
        /// 取消计算流量
        /// </summary>
        /// <param name="relayCache"></param>
        public void RemoveTrafficCache(RelayTrafficCacheInfo relayCache)
        {
            trafficDict.TryRemove(relayCache.Cache.FlowId, out _);
        }
        /// <summary>
        /// 消耗流量
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool AddBytes(RelayTrafficCacheInfo cache, long length)
        {
            Interlocked.Add(ref bytes, length);

            if (Node.DataEachMonth == 0) return true;

            Interlocked.Add(ref cache.Sendt, length);

            return Node.DataRemain > 0;
        }

        /// <summary>
        /// 设置限速
        /// </summary>
        /// <param name="relayCache"></param>
        private void SetLimit(RelayTrafficCacheInfo relayCache)
        {
            //黑白名单
            if (relayCache.Cache.Bandwidth >= 0)
            {
                relayCache.Limit.SetLimit((uint)Math.Ceiling(relayCache.Cache.Bandwidth * 1024 * 1024 / 8.0));
                return;
            }

            relayCache.Limit.SetLimit((uint)Math.Ceiling(Node.Bandwidth * 1024 * 1024 / 8.0));
        }

        private void ResetNodeBytes()
        {
            if (Node.DataEachMonth == 0) return;

            foreach (var cache in trafficDict.Values)
            {
                long length = Interlocked.Exchange(ref cache.Sendt, 0);

                if (Node.DataRemain >= length)
                    relayServerNodeStore.SetMaxGbTotalLastBytes(Node.DataRemain - length);
                else relayServerNodeStore.SetMaxGbTotalLastBytes(0);
            }
            if (Node.DataMonth != DateTime.Now.Month)
            {
                relayServerNodeStore.SetMaxGbTotalMonth(DateTime.Now.Month);
                relayServerNodeStore.SetMaxGbTotalLastBytes((long)(Node.DataEachMonth * 1024 * 1024 * 1024));
            }
            relayServerNodeStore.Confirm();
        }
        private void TrafficTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                try
                {
                    ResetNodeBytes();
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }, 3000);
        }

    }

}
