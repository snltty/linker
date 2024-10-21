using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy
    {
        private ConcurrentDictionary<int, AsyncUserUdpToken> udpListens = new ConcurrentDictionary<int, AsyncUserUdpToken>();
        private ConcurrentDictionary<ulong, UdpConnectedCache> udpConnectds = new ConcurrentDictionary<ulong, UdpConnectedCache>();
        private ConcurrentDictionary<IPEndPoint, UdpTargetCache> udpConnections = new ConcurrentDictionary<IPEndPoint, UdpTargetCache>(new IPEndPointComparer());
        private ConcurrentDictionary<ulong, TaskCompletionSource<IPEndPoint>> udptcss = new ConcurrentDictionary<ulong, TaskCompletionSource<IPEndPoint>>();

        public Func<int, ulong, Task<bool>> UdpConnect { get; set; } = async (port, id) => { return await Task.FromResult(false); };

        #region 服务端

        private void StartUdp(int port, byte bufferSize)
        {
            Socket socketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketUdp.Bind(new IPEndPoint(IPAddress.Any, port));
            AsyncUserUdpToken asyncUserUdpToken = new AsyncUserUdpToken
            {
                ListenPort = port,
                SourceSocket = socketUdp,
            };
            socketUdp.EnableBroadcast = true;
            socketUdp.WindowsUdpBug();

            _ = BindReceive(asyncUserUdpToken, bufferSize);

            udpListens.AddOrUpdate(port, asyncUserUdpToken, (a, b) => asyncUserUdpToken);
        }
        private async Task BindReceive(AsyncUserUdpToken token, byte bufferSize)
        {
            try
            {
                byte[] buffer = new byte[(1 << bufferSize) * 1024];
                IPEndPoint tempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

                string portStr = token.ListenPort.ToString();

                while (true)
                {
                    SocketReceiveFromResult result = await token.SourceSocket.ReceiveFromAsync(buffer, tempRemoteEP).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        break;
                    }

                    Memory<byte> memory = buffer.AsMemory(0, result.ReceivedBytes);

                    AddReceive(portStr, (ulong)memory.Length);

                    IPEndPoint source = result.RemoteEndPoint as IPEndPoint;
                    //已经连接
                    if (udpConnections.TryGetValue(source, out UdpTargetCache cache) && cache != null)
                    {
                        AddSendt(portStr, (ulong)memory.Length);
                        cache.LastTicks.Update();
                        await token.SourceSocket.SendToAsync(memory, cache.IPEndPoint).ConfigureAwait(false);
                    }
                    else
                    {
                        //连接的回复
                        if (memory.Length > flagBytes.Length && memory.Slice(0, flagBytes.Length).Span.SequenceEqual(flagBytes))
                        {
                            ulong _id = memory.Slice(flagBytes.Length).ToUInt64();
                            if (udptcss.TryRemove(_id, out TaskCompletionSource<IPEndPoint> _tcs))
                            {
                                _tcs.SetResult(source);
                            }
                            continue;
                        }

                        if (udpConnections.TryGetValue(source, out _))
                        {
                            continue;
                        }
                        udpConnections.TryAdd(source, null);

                        int length = memory.Length;
                        byte[] buf = ArrayPool<byte>.Shared.Rent(length);
                        memory.CopyTo(buffer);

                        TimerHelper.Async(async () =>
                        {
                            ulong id = ns.Increment();
                            try
                            {
                                if (await UdpConnect(token.ListenPort, id).ConfigureAwait(false))
                                {
                                    TaskCompletionSource<IPEndPoint> tcs = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
                                    udptcss.TryAdd(id, tcs);

                                    IPEndPoint remote = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                                    udpConnections.TryRemove(source, out _);
                                    udpConnections.TryAdd(source, new UdpTargetCache { IPEndPoint = remote });
                                    udpConnections.TryAdd(remote, new UdpTargetCache { IPEndPoint = source });

                                    await token.SourceSocket.SendToAsync(buf.AsMemory(0, length), remote).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                {
                                    LoggerHelper.Instance.Error(ex);
                                }
                            }
                            finally
                            {
                                udptcss.TryRemove(id, out _);
                                ArrayPool<byte>.Shared.Return(buf);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                if (udpListens.TryRemove(token.ListenPort, out token))
                {
                    token.Clear();
                }
            }
        }
        public void StopUdp()
        {
            foreach (var item in udpListens)
            {
                item.Value.Clear();
            }
            udpListens.Clear();
        }
        public virtual void StopUdp(int port)
        {
            if (udpListens.TryRemove(port, out AsyncUserUdpToken udpClient))
            {
                udpClient.Clear();
            }
        }

        #endregion

        /// <summary>
        /// 客户端，收到服务端的udp请求
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <param name="id"></param>
        /// <param name="server"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task OnConnectUdp(byte bufferSize, ulong id, IPEndPoint server, IPEndPoint service)
        {
            Socket socketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketUdp.WindowsUdpBug();

            byte[] buffer = new byte[flagBytes.Length + 8];
            flagBytes.AsMemory().CopyTo(buffer);
            id.ToBytes(buffer.AsMemory(flagBytes.Length));

            await socketUdp.SendToAsync(buffer, server).ConfigureAwait(false);

            Socket serviceUdp = null;
            buffer = new byte[(1 << bufferSize) * 1024];
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);

            UdpConnectedCache cache = new UdpConnectedCache { SourceSocket = socketUdp, TargetSocket = serviceUdp };

            string portStr = service.Port.ToString();
            while (true)
            {
                try
                {
                    SocketReceiveFromResult result = await socketUdp.ReceiveFromAsync(buffer, tempEp).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        serviceUdp?.SafeClose();
                        serviceUdp?.Close();
                        socketUdp?.Dispose();
                        break;
                    }
                    cache.LastTicks.Update();

                    Memory<byte> memory = buffer.AsMemory(0, result.ReceivedBytes);

                    AddReceive(portStr, (ulong)memory.Length);
                    AddSendt(portStr, (ulong)memory.Length);

                    if (serviceUdp == null)
                    {
                        serviceUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        serviceUdp.WindowsUdpBug();
                        await serviceUdp.SendToAsync(memory, service).ConfigureAwait(false);

                        cache.TargetSocket = serviceUdp;
                        udpConnectds.TryAdd(id, cache);

                        TimerHelper.Async(async () =>
                        {
                            byte[] buffer = new byte[(1 << bufferSize) * 1024];
                            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                            while (true)
                            {
                                try
                                {
                                    SocketReceiveFromResult result = await serviceUdp.ReceiveFromAsync(buffer, tempEp).ConfigureAwait(false);
                                    if (result.ReceivedBytes == 0)
                                    {
                                        serviceUdp?.SafeClose();
                                        serviceUdp?.Close();
                                        socketUdp?.Dispose();
                                        break;
                                    }
                                    Memory<byte> memory = buffer.AsMemory(0, result.ReceivedBytes);
                                    AddReceive(portStr, (ulong)memory.Length);
                                    AddSendt(portStr, (ulong)memory.Length);

                                    await socketUdp.SendToAsync(memory, server).ConfigureAwait(false);
                                    cache.LastTicks.Update();
                                }
                                catch (Exception ex)
                                {
                                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    {
                                        LoggerHelper.Instance.Error(ex);
                                    }

                                    serviceUdp?.SafeClose();

                                    serviceUdp?.Close();
                                    socketUdp?.Dispose();

                                    break;
                                }
                            }
                        });
                    }
                    else
                    {
                        await serviceUdp.SendToAsync(memory, service).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }

                    serviceUdp?.Close();
                    socketUdp?.Dispose();

                    break;
                }
            }
        }

        private void UdpTask()
        {
            TimerHelper.SetInterval(() =>
            {
                var connections = udpConnections.Where(c => c.Value.Timeout).Select(c => c.Key);
                foreach (var item in connections)
                {
                    udpConnections.TryRemove(item, out _);
                }

                var connecteds = udpConnectds.Where(c => c.Value.Timeout).Select(c => c.Key);
                foreach (var item in connecteds)
                {
                    if (udpConnectds.TryRemove(item, out UdpConnectedCache cache))
                    {
                        cache.Clear();
                    }
                }
                return true;
            }, 60000);
        }
    }

    public sealed class UdpTargetCache
    {
        public IPEndPoint IPEndPoint { get; set; }
        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();
        public bool Timeout => LastTicks.DiffGreater(5 * 60 * 1000);
    }

    public sealed class UdpConnectedCache
    {
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }
        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();
        public bool Timeout => LastTicks.DiffGreater(5 * 60 * 1000);

        public void Clear()
        {
            SourceSocket?.SafeClose();
            SourceSocket = null;

            TargetSocket?.SafeClose();
            TargetSocket = null;

            GC.Collect();
        }
    }

    public sealed class AsyncUserUdpToken
    {
        public int ListenPort { get; set; }
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }

        public void Clear()
        {
            SourceSocket?.SafeClose();
            SourceSocket = null;

            TargetSocket?.SafeClose();
            TargetSocket = null;

            GC.Collect();
        }
    }

    public sealed class IPEndPointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint x, IPEndPoint y)
        {
            return x.Equals(y);
        }
        public int GetHashCode(IPEndPoint obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }
}
