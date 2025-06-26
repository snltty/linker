using System.Net.Sockets;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using linker.libs;
using System.Text;
using linker.libs.timer;
using linker.messenger.cdkey;

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

        private byte[] relayFlag = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.relay.flag");

        public RelayServerResolver(RelayServerNodeTransfer relayServerNodeTransfer, ISerializer serializer)
        {
            this.relayServerNodeTransfer = relayServerNodeTransfer;
            this.serializer = serializer;
            ClearTask();
        }

        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<Socket>> relayDic = new ConcurrentDictionary<ulong, TaskCompletionSource<Socket>>();
        private readonly ConcurrentDictionary<IPEndPoint, RelayUdpNatInfo> udpNat = new ConcurrentDictionary<IPEndPoint, RelayUdpNatInfo>();
        private readonly ConcurrentDictionary<ulong, RelayUdpNatInfo> relayUdpDic = new ConcurrentDictionary<ulong, RelayUdpNatInfo>();

        public virtual void AddReceive(string key, string from, string to, string groupid, long bytes)
        {
        }
        public virtual void AddSendt(string key, string from, string to, string groupid, long bytes)
        {
        }
        public virtual void AddReceive(string key, long bytes)
        {
        }
        public virtual void AddSendt(string key, long bytes)
        {
        }


        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            if (relayServerNodeTransfer.Validate(tunnel.connection.TunnelProtocolType.Udp) == false)
            {
                return;
            }

            RelayUdpStep step = (RelayUdpStep)memory.Span[0];
            memory = memory.Slice(1);

            if (step == RelayUdpStep.Forward)
            {
                if (udpNat.TryGetValue(ep, out RelayUdpNatInfo natTarget) && natTarget.Target != null)
                {
                    natTarget.LastTicks = Environment.TickCount64;
                    await CopyToAsync(natTarget, socket, ep, memory).ConfigureAwait(false);
                }
                return;
            }

            byte flagLength = memory.Span[0];
            if (memory.Length < flagLength + 1 || memory.Slice(1, flagLength).Span.SequenceEqual(relayFlag) == false)
            {
                await socket.SendToAsync(new byte[] { 1 }, ep).ConfigureAwait(false);
                return;
            }
            memory = memory.Slice(1 + flagLength);

            RelayMessageInfo relayMessage = serializer.Deserialize<RelayMessageInfo>(memory.Span);

            //ask 是发起端来的，那key就是 发起端->目标端， answer的，目标和来源会交换，所以转换一下
            string key = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}->{relayMessage.FlowId}" : $"{relayMessage.ToId}->{relayMessage.FromId}->{relayMessage.FlowId}";
            //获取缓存
            RelayCacheInfo relayCache = await relayServerNodeTransfer.TryGetRelayCache(key).ConfigureAwait(false);
            if (relayCache == null)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"relay {relayMessage.Type} get cache fail,flowid:{relayMessage.FlowId}");
                await socket.SendToAsync(new byte[] { 1 }, ep).ConfigureAwait(false);
                return;
            }

            if (relayMessage.Type == RelayMessengerType.Ask && relayServerNodeTransfer.Validate(relayCache) == false)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"relay {relayMessage.Type} Validate false,flowid:{relayMessage.FlowId}");
                await socket.SendToAsync(new byte[] { 1 }, ep).ConfigureAwait(false);
                return;
            }

            //流量统计
            AddReceive(relayCache.FromId, relayCache.FromName, relayCache.ToName, relayCache.GroupId, memory.Length);
            //回应
            if (relayMessage.Type == RelayMessengerType.Answer)
            {
                if (relayUdpDic.TryRemove(relayCache.FlowId, out RelayUdpNatInfo natAsk))
                {
                    natAsk.Target = ep;

                    RelayUdpNatInfo natAnswer = new RelayUdpNatInfo { Target = natAsk.Source, Traffic = natAsk.Traffic, Source = ep };
                    udpNat.AddOrUpdate(ep, natAnswer, (a, b) => natAnswer);
                }
                return;
            }

            //请求
            RelayTrafficCacheInfo trafficCacheInfo = new RelayTrafficCacheInfo { Cache = relayCache, Sendt = 0, Limit = new RelaySpeedLimit() };
            RelayUdpNatInfo nat = new RelayUdpNatInfo { Ask = true, Source = ep, Traffic = trafficCacheInfo };
            udpNat.AddOrUpdate(ep, nat, (a, b) => nat);
            relayUdpDic.TryAdd(relayCache.FlowId, nat);

            relayServerNodeTransfer.AddTrafficCache(trafficCacheInfo);
            relayServerNodeTransfer.IncrementConnectionNum();

            await socket.SendToAsync(new byte[] { 0 }, ep).ConfigureAwait(false);

        }
        private async Task CopyToAsync(RelayUdpNatInfo nat, Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            RelayTrafficCacheInfo trafficCacheInfo = nat.Traffic;
            int bytesRead = memory.Length;

            //流量限制
            if (relayServerNodeTransfer.AddBytes(trafficCacheInfo, bytesRead) == false)
            {
                return;
            }
            //总速度
            if (relayServerNodeTransfer.NeedLimit(trafficCacheInfo) && relayServerNodeTransfer.TryLimitPacket(bytesRead) == false)
            {
                return;
            }
            //单个速度
            if (trafficCacheInfo.Limit.NeedLimit() && trafficCacheInfo.Limit.TryLimitPacket(bytesRead) == false)
            {
                return;
            }
            AddReceive(trafficCacheInfo.Cache.FromId, trafficCacheInfo.Cache.FromName, trafficCacheInfo.Cache.ToName, trafficCacheInfo.Cache.GroupId, bytesRead);
            AddSendt(trafficCacheInfo.Cache.FromId, trafficCacheInfo.Cache.FromName, trafficCacheInfo.Cache.ToName, trafficCacheInfo.Cache.GroupId, bytesRead);
            await socket.SendToAsync(memory, nat.Target).ConfigureAwait(false);
        }


        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            if (relayServerNodeTransfer.Validate(tunnel.connection.TunnelProtocolType.Tcp) == false)
            {
                socket.SafeClose();
                return;
            }

            byte[] buffer1 = new byte[8 * 1024];
            try
            {
                int length = await socket.ReceiveAsync(buffer1.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                RelayMessageInfo relayMessage = serializer.Deserialize<RelayMessageInfo>(buffer1.AsMemory(0, length).Span);

                //ask 是发起端来的，那key就是 发起端->目标端， answer的，目标和来源会交换，所以转换一下
                string key = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}->{relayMessage.FlowId}" : $"{relayMessage.ToId}->{relayMessage.FromId}->{relayMessage.FlowId}";
                //获取缓存
                RelayCacheInfo relayCache = await relayServerNodeTransfer.TryGetRelayCache(key).ConfigureAwait(false);
                if (relayCache == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay {relayMessage.Type} get cache fail,flowid:{relayMessage.FlowId}");
                    await socket.SendAsync(new byte[] { 1 }).ConfigureAwait(false);
                    socket.SafeClose();
                    return;
                }

                if (relayMessage.Type == RelayMessengerType.Ask && relayServerNodeTransfer.Validate(relayCache) == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay {relayMessage.Type} validate false,flowid:{relayMessage.FlowId}");
                    await socket.SendAsync(new byte[] { 1 }).ConfigureAwait(false);
                    socket.SafeClose();
                    return;
                }

                //流量统计
                AddReceive(relayCache.FromId, relayCache.FromName, relayCache.ToName, relayCache.GroupId, length);

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
                    await socket.SendAsync(new byte[] { 0 }).ConfigureAwait(false);


                    relayDic.TryAdd(relayCache.FlowId, tcs);
                    Socket answerSocket = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);

                    byte[] buffer2 = new byte[8 * 1024];
                    RelayTrafficCacheInfo trafficCacheInfo = new RelayTrafficCacheInfo { Cache = relayCache, Sendt = 0, Limit = new RelaySpeedLimit() };
                    relayServerNodeTransfer.AddTrafficCache(trafficCacheInfo);
                    relayServerNodeTransfer.IncrementConnectionNum();
                    await Task.WhenAll(CopyToAsync(trafficCacheInfo, socket, answerSocket, buffer1), CopyToAsync(trafficCacheInfo, answerSocket, socket, buffer2)).ConfigureAwait(false);
                    relayServerNodeTransfer.DecrementConnectionNum();
                    relayServerNodeTransfer.RemoveTrafficCache(trafficCacheInfo);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(null);
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"{ex},flowid:{relayMessage.FlowId}");
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
        private async Task CopyToAsync(RelayTrafficCacheInfo trafficCacheInfo, Socket source, Socket destination, Memory<byte> memory)
        {
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(memory, SocketFlags.None).ConfigureAwait(false)) != 0)
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

                    AddReceive(trafficCacheInfo.Cache.FromId, trafficCacheInfo.Cache.FromName, trafficCacheInfo.Cache.ToName, trafficCacheInfo.Cache.GroupId, bytesRead);
                    AddSendt(trafficCacheInfo.Cache.FromId, trafficCacheInfo.Cache.FromName, trafficCacheInfo.Cache.ToName, trafficCacheInfo.Cache.GroupId, bytesRead);
                    await destination.SendAsync(memory.Slice(0, bytesRead), SocketFlags.None).ConfigureAwait(false);
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

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                try
                {
                    long ticks = Environment.TickCount64;
                    foreach (var item in udpNat.Values.Where(c => c.Ask && ticks - c.LastTicks > 30000).ToList())
                    {
                        relayServerNodeTransfer.DecrementConnectionNum();
                        relayServerNodeTransfer.RemoveTrafficCache(item.Traffic);

                        relayUdpDic.TryRemove(item.Traffic.Cache.FlowId, out _);

                        udpNat.TryRemove(item.Source, out _);
                        if (item.Target != null)
                        {
                            udpNat.TryRemove(item.Target, out _);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }, 5000);
        }
    }

    public enum RelayUdpStep : byte
    {
        Connect = 0,
        Forward = 1,
    }
    public sealed class RelayUdpNatInfo
    {
        public bool Ask { get; set; }
        public IPEndPoint Source { get; set; }
        public IPEndPoint Target { get; set; }
        public long LastTicks { get; set; } = Environment.TickCount64;
        public RelayTrafficCacheInfo Traffic { get; set; }
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

    public sealed partial class RelayCacheInfo
    {
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string FromName { get; set; }
        public string ToId { get; set; }
        public string ToName { get; set; }
        public string GroupId { get; set; }
        public string UserId { get; set; }
        public bool Validated { get; set; }

        public bool UseCdkey { get; set; }
        public List<CdkeyInfo> Cdkey { get; set; } = [];
    }
    public sealed class RelayTrafficCacheInfo
    {
        public long Sendt;
        public long SendtCache;
        public RelaySpeedLimit Limit { get; set; }
        public RelayCacheInfo Cache { get; set; }
        public CdkeyInfo CurrentCdkey { get; set; }
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