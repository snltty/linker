using System.Net.Sockets;
using System.Buffers;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using linker.libs;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继连接处理
    /// </summary>
    public class RelayServerResolver : IResolver
    {
        public byte Type => (byte)ResolverType.Relay;

        private readonly RelayServerNodeTransfer relayServerNodeTransfer;
        private readonly ISerializer serializer;
        public RelayServerResolver(RelayServerNodeTransfer relayServerNodeTransfer, ISerializer serializer)
        {
            this.relayServerNodeTransfer = relayServerNodeTransfer;
            this.serializer = serializer;
        }

        private readonly ConcurrentDictionary<ulong, RelayWrapInfo> relayDic = new ConcurrentDictionary<ulong, RelayWrapInfo>();



        public virtual void AddReceive(string key, string from, string to, string groupid, ulong bytes)
        {
        }
        public virtual void AddSendt(string key, string from, string to, string groupid, ulong bytes)
        {
        }
        public virtual void AddReceive(string key, ulong bytes)
        {
        }
        public virtual void AddSendt(string key, ulong bytes)
        {
        }


        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }
        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                RelayMessageInfo relayMessage = serializer.Deserialize<RelayMessageInfo>(buffer.AsMemory(0, length).Span);

                if (relayMessage.Type == RelayMessengerType.Ask && relayMessage.NodeId != RelayServerNodeInfo.MASTER_NODE_ID)
                {
                    if (relayServerNodeTransfer.Validate() == false)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"relay {relayMessage.Type} Validate false,flowid:{relayMessage.FlowId}");
                        await socket.SendAsync(new byte[] { 1 });
                        socket.SafeClose();
                        return;
                    }
                }
                //ask 是发起端来的，那key就是 发起端->目标端， answer的，目标和来源会交换，所以转换一下
                string key = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}->{relayMessage.FlowId}" : $"{relayMessage.ToId}->{relayMessage.FromId}->{relayMessage.FlowId}";
                //获取缓存
                RelayCacheInfo relayCache = await relayServerNodeTransfer.TryGetRelayCache(key, relayMessage.NodeId);
                if (relayCache == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay {relayMessage.Type} get cache fail,flowid:{relayMessage.FlowId}");
                    socket.SafeClose();
                    return;
                }
                //流量统计
                AddReceive(relayCache.FromId, relayCache.FromName, relayCache.ToName, relayCache.GroupId, (ulong)length);
                try
                {
                    switch (relayMessage.Type)
                    {
                        case RelayMessengerType.Ask:
                            {
                                //添加本地缓存
                                RelayWrapInfo relayWrap = new RelayWrapInfo { Socket = socket, Tcs = new TaskCompletionSource() };
                                relayWrap.Limit.SetLimit(relayServerNodeTransfer.GetBandwidthLimit());

                                relayDic.TryAdd(relayCache.FlowId, relayWrap);

                                await socket.SendAsync(new byte[] { 0 });
                                //等待对方连接
                                await relayWrap.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(15000));
                            }
                            break;
                        case RelayMessengerType.Answer:
                            {
                                //看发起端缓存
                                if (relayDic.TryRemove(relayCache.FlowId, out RelayWrapInfo relayWrap) == false || relayWrap.Socket == null)
                                {
                                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                        LoggerHelper.Instance.Error($"relay {relayMessage.Type} get cache fail,flowid:{relayMessage.FlowId}");
                                    socket.SafeClose();
                                    return;
                                }
                                relayWrap.Tcs.SetResult();

                                await Task.WhenAll([
                                     CopyToAsync(relayCache, relayWrap.Limit, socket, relayWrap.Socket, relayMessage.NodeId != RelayServerNodeInfo.MASTER_NODE_ID),
                                     CopyToAsync(relayCache, relayWrap.Limit,relayWrap.Socket, socket,  relayMessage.NodeId != RelayServerNodeInfo.MASTER_NODE_ID),
                                ]);
                            }
                            break;
                        default:
                            {
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    LoggerHelper.Instance.Error($"relay {relayMessage.Type}  unknow type,flowid:{relayMessage.FlowId}");
                                socket.SafeClose();
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"{ex},flowid:{relayMessage.FlowId}");
                    if (relayDic.TryRemove(relayCache.FlowId, out RelayWrapInfo remove))
                    {
                        remove.Socket?.SafeClose();
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                socket.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private async Task CopyToAsync(RelayCacheInfo cache, RelaySpeedLimit limit, Socket source, Socket destination, bool needLimit)
        {
            byte[] buffer = new byte[8 * 1024];
            try
            {
                relayServerNodeTransfer.IncrementConnectionNum();

                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    //流量限制
                    if (relayServerNodeTransfer.AddBytes((ulong)bytesRead) == false)
                    {
                        source.SafeClose();
                        break;
                    }

                    //总速度
                    if (needLimit && relayServerNodeTransfer.NeedLimit())
                    {
                        int length = bytesRead;
                        relayServerNodeTransfer.TryLimit(ref length);
                        while (length > 0)
                        {
                            await Task.Delay(30).ConfigureAwait(false);
                            relayServerNodeTransfer.TryLimit(ref length);
                        }
                    }
                    //单个速度
                    if (needLimit && limit.NeedLimit())
                    {
                        int length = bytesRead;
                        limit.TryLimit(ref length);
                        while (length > 0)
                        {
                            await Task.Delay(30).ConfigureAwait(false);
                            limit.TryLimit(ref length);
                        }
                    }

                    AddReceive(cache.FromId, cache.FromName, cache.ToName, cache.GroupId, (ulong)bytesRead);
                    AddSendt(cache.FromId, cache.FromName, cache.ToName, cache.GroupId, (ulong)bytesRead);
                    await destination.SendAsync(buffer.AsMemory(0, bytesRead),SocketFlags.None).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                relayServerNodeTransfer.DecrementConnectionNum();
                source.SafeClose();
                destination.SafeClose();
            }
        }
    }


    public enum RelayMessengerType : byte
    {
        Ask = 0,
        Answer = 1,
    }
    public class RelaySpeedLimit
    {
        private uint relayLimit = 0;
        private double relayLimitToken = 0;
        private double relayLimitBucket = 0;
        private long relayLimitTicks = Environment.TickCount64;

        public bool NeedLimit()
        {
            return relayLimit > 0;
        }
        public void SetLimit(uint bytes)
        {
            relayLimit = bytes;
            relayLimitToken = relayLimit / 1000.0;
            relayLimitBucket = relayLimit;
        }
        public bool TryLimit(ref int length)
        {
            if (relayLimit == 0) return false;

            lock (this)
            {
                long _relayLimitTicks = Environment.TickCount64;
                long relayLimitTicksTemp = _relayLimitTicks - relayLimitTicks;
                relayLimitTicks = _relayLimitTicks;
                relayLimitBucket += relayLimitTicksTemp * relayLimitToken;
                if (relayLimitBucket > relayLimit) relayLimitBucket = relayLimit;

                if (relayLimitBucket >= length)
                {
                    relayLimitBucket -= length;
                    length = 0;
                }
                else
                {
                    length -= (int)relayLimitBucket;
                    relayLimitBucket = 0;
                }
            }
            return true;
        }
    }

    public sealed partial class RelayCacheInfo
    {
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string FromName { get; set; }
        public string ToId { get; set; }
        public string ToName { get; set; }
        public string GroupId { get; set; }
    }

    public sealed class RelayWrapInfo
    {
        public TaskCompletionSource Tcs { get; set; }
        public Socket Socket { get; set; }
        public RelaySpeedLimit Limit { get; set; } = new RelaySpeedLimit();
    }

    public sealed partial class RelayMessageInfo
    {
        public RelayMessengerType Type { get; set; }
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }

        public string NodeId { get; set; }
    }
}