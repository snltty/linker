using linker.libs;
using linker.libs.extends;
using linker.messenger.node;
using linker.tunnel.transport;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

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
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int received = 0, length = 4;
                while (received < length)
                {
                    received += await socket.ReceiveAsync(buffer.AsMemory(received, length - received), SocketFlags.None, cts.Token).ConfigureAwait(false);
                }

                received = 0;
                length = buffer.ToInt32();
                while (received < length)
                {
                    received += await socket.ReceiveAsync(buffer.AsMemory(received, length - received), SocketFlags.None, cts.Token).ConfigureAwait(false);
                }

                return serializer.Deserialize<RelayMessageInfo>(crypto.Decode(buffer, 0, length).Span);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
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
                RelayMessageInfo relayMessage = await GetMessage(socket).ConfigureAwait(false);
                if(relayMessage == null)
                {
                    return;
                }
                //获取缓存
                RelayCacheInfo relayCache = await relayServerNodeTransfer.TryGetRelayCache(relayMessage).ConfigureAwait(false);
                if (relayCache == null)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay server {relayMessage.Type} get cache fail,flowid:{relayMessage.FlowId}");
                    await socket.SendAsync(Helper.FalseArray).ConfigureAwait(false);
                    socket.SafeClose();
                    return;
                }

                if (relayMessage.Type == RelayMessengerType.Ask && relayServerNodeTransfer.Validate(relayCache) == false)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay server {relayMessage.Type} validate false,flowid:{relayMessage.FlowId}");
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
                Socket answerSocket = null;
                IPEndPoint fromep = socket.RemoteEndPoint as IPEndPoint, toep = null;
                try
                {
                    await socket.SendAsync(Helper.TrueArray).ConfigureAwait(false);
                    relayDic.TryAdd(relayCache.FlowId, tcs);
                    answerSocket = await tcs.WithTimeout(TimeSpan.FromMilliseconds(15000)).ConfigureAwait(false);
                    await answerSocket.SendAsync(Helper.TrueArray).ConfigureAwait(false);
                    toep = answerSocket.RemoteEndPoint as IPEndPoint;

                    LoggerHelper.Instance.Info($"relay server start {fromep} to {toep}");

                    string flowKey = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}" : $"{relayMessage.ToId}->{relayMessage.FromId}";
                    RelayTrafficCacheInfo trafficCacheInfo = new RelayTrafficCacheInfo { Cache1 = relayCache, Cache = relayCache, Sendt = 0, Limit = new SpeedLimit(), Key = flowKey };
                    relayServerNodeTransfer.AddTrafficCache(trafficCacheInfo);
                    relayServerNodeTransfer.IncrementConnectionNum();
                    await Task.WhenAll(CopyToAsync(trafficCacheInfo, socket, answerSocket), CopyToAsync(trafficCacheInfo, answerSocket, socket)).ConfigureAwait(false);
                    relayServerNodeTransfer.DecrementConnectionNum();
                    relayServerNodeTransfer.RemoveTrafficCache(trafficCacheInfo);
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(null);
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Error($"relay server error {ex},flowid:{relayMessage.FlowId}");
                }
                finally
                {
                    LoggerHelper.Instance.Info($"relay server end {fromep} to {toep}");
                    relayDic.TryRemove(relayCache.FlowId, out _);
                    socket?.SafeClose();
                    answerSocket?.SafeClose();
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                socket?.SafeClose();
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

                    Add(trafficCacheInfo.Key, trafficCacheInfo.Cache1.FromName, trafficCacheInfo.Cache1.ToName, trafficCacheInfo.Cache1.GroupId, bytesRead, bytesRead);
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
    public sealed partial class RelayCacheInfo : CacheInfo
    {
        public string FromId { get; set; }
        public string FromName { get; set; }
        public string ToId { get; set; }
        public string ToName { get; set; }
        public string GroupId { get; set; }

        public string UserId { get; set; } = string.Empty;
    }
    public sealed class RelayTrafficCacheInfo : TrafficCacheInfo
    {
        public RelayCacheInfo Cache1 { get; set; }
    }
}