using System.Net.Sockets;
using System.Buffers;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.plugins.resolver;
using System.Net;
using linker.plugins.relay.caching;
using MemoryPack;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继连接处理
    /// </summary>
    public class RelayResolver : IResolver
    {
        public ResolverType Type => ResolverType.Relay;

        private readonly IRelayCaching relayCaching;
        public RelayResolver(IRelayCaching relayCaching)
        {
            this.relayCaching = relayCaching;
        }

        private readonly ConcurrentDictionary<ulong, RelayWrap> relayDic = new ConcurrentDictionary<ulong, RelayWrap>();
        private ulong relayFlowingId = 0;
        public async Task<ulong> NewRelay(string fromid, string fromName, string toid, string toName)
        {
            ulong flowingId = Interlocked.Increment(ref relayFlowingId);

            RelayCache cache = new RelayCache
            {
                FlowId = flowingId,
                FromId = fromid,
                FromName = fromName,
                ToId = toid,
                ToName = toName
            };
            bool added = await relayCaching.TryAdd($"{fromid}->{toid}", cache, 5000);
            if (added == false) return 0;

            return flowingId;
        }


        public virtual void AddReceive(string key, string from, string to, ulong bytes)
        {
        }
        public virtual void AddSendt(string key, string from, string to, ulong bytes)
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
                RelayMessage relayMessage = RelayMessage.FromBytes(buffer.AsMemory(0, length));

                //ask 是发起端来的，那key就是 发起端->目标端， answer的，目标和来源会交换，所以转换一下
                string key = relayMessage.Type == RelayMessengerType.Ask ? $"{relayMessage.FromId}->{relayMessage.ToId}" : $"{relayMessage.ToId}->{relayMessage.FromId}";
                RelayCachingValue<RelayCache> relayCacheWrap = new RelayCachingValue<RelayCache>();
                if (await relayCaching.TryGetValue(key, relayCacheWrap) == false)
                {
                    socket.SafeClose();
                    return;
                }
                //流量统计
                AddReceive(relayCacheWrap.Value.FromId, relayCacheWrap.Value.FromName, relayCacheWrap.Value.ToName, (ulong)length);
                try
                {
                    switch (relayMessage.Type)
                    {
                        case RelayMessengerType.Ask:
                            break;
                        case RelayMessengerType.Answer:
                            break;
                        default:
                            break;
                    }
                    //发起端
                    if (relayMessage.Type == RelayMessengerType.Ask)
                    {
                        //添加本地缓存
                        RelayWrap relayWrap = new RelayWrap { Socket = socket, Tcs = new TaskCompletionSource<Socket>() };
                        relayDic.TryAdd(relayCacheWrap.Value.FlowId, relayWrap);

                        //等待对方连接
                        Socket targetSocket = await relayWrap.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(3000));
                        _ = CopyToAsync(relayCacheWrap.Value.FromId, relayCacheWrap.Value.FromName, relayCacheWrap.Value.ToName, 3, socket, targetSocket);
                    }
                    else if (relayMessage.Type == RelayMessengerType.Answer)
                    {
                        //看发起端缓存
                        if (relayDic.TryRemove(relayCacheWrap.Value.FlowId, out RelayWrap relayWrap) == false || relayWrap.Socket == null)
                        {
                            socket.SafeClose();
                            return;
                        }
                        //告诉发起端我的socket
                        relayWrap.Tcs.SetResult(socket);
                        _ = CopyToAsync(relayCacheWrap.Value.FromId, relayCacheWrap.Value.FromName, relayCacheWrap.Value.ToName, 3, socket, relayWrap.Socket);
                    }
                }
                catch (Exception)
                {
                    if (relayDic.TryRemove(relayCacheWrap.Value.FlowId, out RelayWrap remove))
                    {
                        remove.Socket?.SafeClose();
                    }
                }
            }
            catch (Exception)
            {
                socket.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        private async Task CopyToAsync(string fromid, string fromName, string toName, byte bufferSize, Socket source, Socket destination)
        {
            byte[] buffer = new byte[(1 << bufferSize) * 1024];
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory()).ConfigureAwait(false)) != 0)
                {
                    AddReceive(fromid, fromName, toName, (ulong)bytesRead);
                    AddSendt(fromid, fromName, toName, (ulong)bytesRead);
                    await destination.SendAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }
        }
    }


    public enum RelayMessengerType : byte
    {
        Ask = 0,
        Answer = 1,
    }

    [MemoryPackable]
    public sealed partial class RelayCache
    {
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string FromName { get; set; }
        public string ToId { get; set; }
        public string ToName { get; set; }
    }
    public sealed class RelayWrap
    {
        public TaskCompletionSource<Socket> Tcs { get; set; }
        public Socket Socket { get; set; }
    }

    public sealed class RelayMessage
    {
        public RelayMessengerType Type { get; set; }
        public ulong FlowId { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }

        public byte[] ToBytes()
        {
            byte[] fromidBytes = FromId.ToBytes();
            byte[] toidBytes = ToId.ToBytes();

            byte[] bytes = new byte[1 + 1 + 8 + 1 + fromidBytes.Length + 1 + toidBytes.Length];

            int index = 0;
            bytes[index] = (byte)ResolverType.Relay;
            index++;

            bytes[index] = (byte)Type;
            index++;

            FlowId.ToBytes(bytes.AsMemory(index));
            index += 8;

            bytes[index] = (byte)fromidBytes.Length;
            index++;
            fromidBytes.AsMemory().CopyTo(bytes.AsMemory(index));
            index += fromidBytes.Length;


            bytes[index] = (byte)toidBytes.Length;
            index++;
            toidBytes.AsMemory().CopyTo(bytes.AsMemory(index));
            index += toidBytes.Length;


            return bytes;
        }

        public static RelayMessage FromBytes(Memory<byte> memory)
        {
            try
            {
                RelayMessage relayMessage = new RelayMessage();
                var span = memory.Span;

                int index = 0;
                relayMessage.Type = (RelayMessengerType)span[index];
                index++;

                relayMessage.FlowId = span.Slice(index, 8).ToUInt64();
                index += 8;

                int length = span[index];
                index++;
                relayMessage.FromId = span.Slice(index, length).GetString();
                index += length;

                length = span[index];
                index++;
                relayMessage.ToId = span.Slice(index, length).GetString();
                index += length;

                return relayMessage;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}