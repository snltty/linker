using System.Net.Sockets;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using linker.libs;
using System.Buffers;
using linker.tunnel.transport;

namespace linker.messenger.relay.server
{
    /// <summary>
    /// 中继连接处理
    /// </summary>
    public class RelayServerResolver : IResolver
    {
        public byte Type => (byte)ResolverType.Relay;

        private readonly ICrypto crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

        private readonly RelayServerNodeTransfer relayServerNodeTransfer;
        private readonly ISerializer serializer;

        public RelayServerResolver(RelayServerNodeTransfer relayServerNodeTransfer, ISerializer serializer)
        {
            this.relayServerNodeTransfer = relayServerNodeTransfer;
            this.serializer = serializer;
        }

        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<Socket>> relayDic = new();

        public virtual void Add(string key, string from, string to, string groupid, long receiveBytes, long sendtBytes)
        {
        }
        public virtual void Add(string key, long receiveBytes, long sendtBytes)
        {
        }
        public virtual long GetReceive()
        {
            return 0;
        }
        public virtual long GetSent()
        {
            return 0;
        }

        private async Task<RelayMessageInfo> GetMessage(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int received = 0, length = 4;
                while (received < length)
                {
                    received += await socket.ReceiveAsync(buffer.AsMemory(received, length - received), SocketFlags.None).ConfigureAwait(false);
                }

                received = 0;
                length = buffer.ToInt32();
                while (received < length)
                {
                    received += await socket.ReceiveAsync(buffer.AsMemory(received, length - received), SocketFlags.None).ConfigureAwait(false);
                }

                return serializer.Deserialize<RelayMessageInfo>(crypto.Decode(buffer, 0, length).Span);
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return null;
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }
        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            if (relayServerNodeTransfer.Validate(tunnel.connection.TunnelProtocolType.Tcp) == false)
            {
                socket.SafeClose();
                return;
            }

            try
            {
                RelayMessageInfo relayMessage = await GetMessage(socket).WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                //获取缓存
                RelayCacheInfo relayCache = await relayServerNodeTransfer.TryGetRelayCache(relayMessage).ConfigureAwait(false);
                if (relayCache == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay {relayMessage.Type} get cache fail,flowid:{relayMessage.FlowId}");
                    await socket.SendAsync(Helper.FalseArray).ConfigureAwait(false);
                    socket.SafeClose();
                    return;
                }

                if (relayMessage.Type == RelayMessengerType.Ask && relayServerNodeTransfer.Validate(relayCache) == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay {relayMessage.Type} validate false,flowid:{relayMessage.FlowId}");
                    await socket.SendAsync(Helper.FalseArray).ConfigureAwait(false);
                    socket.SafeClose();
                    return;
                }

                if (relayMessage.Type == RelayMessengerType.Answer)
                {
                    if (relayDic.TryRemove(relayCache.FlowId, out TaskCompletionSource<Socket> tcsAsk))
                    {
                        tcsAsk.TrySetResult(socket);
                    }
                    else
                    {
                        socket.SafeClose();
                    }
                    return;
                }

                TaskCompletionSource<Socket> tcs = new TaskCompletionSource<Socket>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    await socket.SendAsync(Helper.TrueArray).ConfigureAwait(false);
                    relayDic.TryAdd(relayCache.FlowId, tcs);
                    Socket answerSocket = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);
                    await answerSocket.SendAsync(Helper.TrueArray).ConfigureAwait(false);

                    LoggerHelper.Instance.Error($"relay start {socket.RemoteEndPoint} to {answerSocket.RemoteEndPoint}");

                    string flowKey = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}" : $"{relayMessage.ToId}->{relayMessage.FromId}";
                    RelayTrafficCacheInfo trafficCacheInfo = new RelayTrafficCacheInfo { Cache = relayCache, Sendt = 0, Limit = new RelaySpeedLimit(), Key = flowKey };
                    relayServerNodeTransfer.AddTrafficCache(trafficCacheInfo);
                    relayServerNodeTransfer.IncrementConnectionNum();
                    await Task.WhenAll(CopyToAsync(trafficCacheInfo, socket, answerSocket), CopyToAsync(trafficCacheInfo, answerSocket, socket)).ConfigureAwait(false);
                    relayServerNodeTransfer.DecrementConnectionNum();
                    relayServerNodeTransfer.RemoveTrafficCache(trafficCacheInfo);

                    LoggerHelper.Instance.Error($"relay end {socket.RemoteEndPoint} to {answerSocket.RemoteEndPoint}");
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(null);
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"{ex},flowid:{relayMessage.FlowId}");
                }
                finally
                {
                    relayDic.TryRemove(relayCache.FlowId, out _);
                    socket.SafeClose();
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                socket.SafeClose();
            }
        }
        private async Task CopyToAsync(RelayTrafficCacheInfo trafficCacheInfo, Socket source, Socket destination)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.Memory, SocketFlags.None).ConfigureAwait(false)) != 0)
                {
                    //流量限制
                    if (relayServerNodeTransfer.AddBytes(trafficCacheInfo, bytesRead) == false)
                    {
                        source.SafeClose();
                        break;
                    }

                    //总速度
                    if (relayServerNodeTransfer.NeedLimit(trafficCacheInfo))
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
                    if (trafficCacheInfo.Limit.NeedLimit())
                    {
                        int length = bytesRead;
                        trafficCacheInfo.Limit.TryLimit(ref length);
                        while (length > 0)
                        {
                            await Task.Delay(30).ConfigureAwait(false);
                            trafficCacheInfo.Limit.TryLimit(ref length);
                        }
                    }

                    Add(trafficCacheInfo.Key, trafficCacheInfo.Cache.FromName, trafficCacheInfo.Cache.ToName, trafficCacheInfo.Cache.GroupId, bytesRead, bytesRead);
                    await destination.SendAsync(buffer.Memory.Slice(0, bytesRead), SocketFlags.None).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
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
            //每s多少字节
            relayLimit = bytes;
            //每ms多少字节
            relayLimitToken = relayLimit / 1000.0;
            //桶里有多少字节
            relayLimitBucket = relayLimit;
        }
        public bool TryLimit(ref int length)
        {
            //0不限速
            if (relayLimit == 0) return true;

            lock (this)
            {
                long _relayLimitTicks = Environment.TickCount64;
                //距离上次经过了多少ms
                long relayLimitTicksTemp = _relayLimitTicks - relayLimitTicks;
                relayLimitTicks = _relayLimitTicks;
                //桶里增加多少字节
                relayLimitBucket += relayLimitTicksTemp * relayLimitToken;
                //桶溢出了
                if (relayLimitBucket > relayLimit) relayLimitBucket = relayLimit;

                //能全部消耗调
                if (relayLimitBucket >= length)
                {
                    relayLimitBucket -= length;
                    length = 0;
                }
                else
                {
                    //只能消耗一部分
                    length -= (int)relayLimitBucket;
                    relayLimitBucket = 0;
                }
            }
            return true;
        }
        public bool TryLimitPacket(int length)
        {
            if (relayLimit == 0) return true;

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
                    return true;
                }
            }
            return false;
        }
    }

    public sealed class RelayTrafficCacheInfo
    {
        public long Sendt;
        public long SendtCache;
        public RelaySpeedLimit Limit { get; set; }
        public RelayCacheInfo Cache { get; set; }
        public string Key { get; set; }
    }
}