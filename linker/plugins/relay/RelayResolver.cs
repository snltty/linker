using System.Net.Sockets;
using System.Buffers;
using linker.libs.extends;
using System.Collections.Concurrent;
using linker.plugins.resolver;
using System.Net;

namespace linker.plugins.relay
{
    /// <summary>
    /// 中继连接处理
    /// </summary>
    public sealed class RelayResolver : IResolver
    {
        public ResolverType Type => ResolverType.Relay;
        public ulong ReceiveBytes { get; private set; }
        public ulong SendtBytes { get; private set; }

        public RelayResolver()
        {
        }

        private readonly ConcurrentDictionary<ulong, RelayWrap> relayDic = new ConcurrentDictionary<ulong, RelayWrap>();
        private ulong relayFlowingId = 0;
        public ulong NewRelay(string fromid, string toid)
        {
            ulong flowingId = Interlocked.Increment(ref relayFlowingId);

            RelayWrap relayWrap = new RelayWrap { FlowId = flowingId, Tcs = new TaskCompletionSource<Socket>(), WaitTcs = new TaskCompletionSource<Socket>(), Socket = null, FromId = fromid, ToId = toid };
            relayDic.TryAdd(flowingId, relayWrap);

            relayWrap.WaitTcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000)).ContinueWith((result) =>
            {
                if (result.IsFaulted)
                {
                    relayDic.TryRemove(flowingId, out _);
                }
            });

            return flowingId;
        }

        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }
        public async Task Resolve(Socket socket)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).ConfigureAwait(false);
                ReceiveBytes += (ulong)length;
                RelayMessage relayMessage = RelayMessage.FromBytes(buffer.AsMemory(0, length));

                if (relayDic.TryGetValue(relayMessage.FlowId, out RelayWrap relayWrap) == false)
                {
                    socket.SafeClose();
                    return;
                }
                try
                {
                    if (relayMessage.Type == RelayMessengerType.Ask)
                    {
                        if (relayMessage.FromId != relayWrap.FromId)
                        {
                            socket.SafeClose();
                            return;
                        }
                        relayWrap.WaitTcs.SetResult(null);
                        relayWrap.Socket = socket;
                        Socket targetSocket = await relayWrap.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(3000));
                        _ = CopyToAsync(3, socket, targetSocket);
                    }
                    else if (relayMessage.Type == RelayMessengerType.Answer && relayWrap.Socket != null)
                    {
                        if (relayMessage.FromId != relayWrap.ToId)
                        {
                            socket.SafeClose();
                            return;
                        }

                        relayWrap.Tcs.SetResult(socket);
                        _ = CopyToAsync(3, socket, relayWrap.Socket);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    relayDic.TryRemove(relayMessage.FlowId, out _);
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
        private async Task CopyToAsync(byte bufferSize, Socket source, Socket destination)
        {
            byte[] buffer = new byte[(1 << bufferSize) * 1024];
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer.AsMemory()).ConfigureAwait(false)) != 0)
                {
                    ReceiveBytes += (ulong)bytesRead;
                    SendtBytes += (ulong)bytesRead;
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


    public sealed class RelayWrap
    {
        public ulong FlowId { get; set; }
        public TaskCompletionSource<Socket> Tcs { get; set; }
        public TaskCompletionSource<Socket> WaitTcs { get; set; }
        public Socket Socket { get; set; }

        public string FromId { get; set; }
        public string ToId { get; set; }
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