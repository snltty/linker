using cmonitor.plugins.sforward.config;
using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace cmonitor.plugins.sforward
{
    public partial class SForwardProxy
    {
        private ConcurrentDictionary<int, AsyncUserUdpToken> udpListens = new ConcurrentDictionary<int, AsyncUserUdpToken>();
        private ConcurrentDictionary<IPEndPoint, UdpTargetCache> udpConnections = new ConcurrentDictionary<IPEndPoint, UdpTargetCache>(new IPEndPointComparer());
        private ConcurrentDictionary<ulong, TaskCompletionSource<IPEndPoint>> udptcss = new ConcurrentDictionary<ulong, TaskCompletionSource<IPEndPoint>>();

        public Func<int, ulong, Task<bool>> UdpConnect { get; set; } = async (port, id) => { return await Task.FromResult(false); };

        private void StartUdp(int port)
        {
            UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            AsyncUserUdpToken asyncUserUdpToken = new AsyncUserUdpToken
            {
                ListenPort = port,
                SourceSocket = udpClient,
            };
            udpClient.Client.EnableBroadcast = true;
            udpClient.Client.WindowsUdpBug();

            _ = BindReceive(asyncUserUdpToken);

            udpListens.AddOrUpdate(port, asyncUserUdpToken, (a, b) => asyncUserUdpToken);
        }

        private async Task BindReceive(AsyncUserUdpToken token)
        {
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await token.SourceSocket.ReceiveAsync();
                    if (result.Buffer.Length == 0) break;

                    //已经连接
                    if (udpConnections.TryGetValue(result.RemoteEndPoint, out UdpTargetCache cache))
                    {
                        await token.SourceSocket.SendAsync(result.Buffer, cache.IPEndPoint);
                    }
                    else
                    {
                        //连接的回复
                        if (result.Buffer.Length > flagBytes.Length && result.Buffer.AsSpan(0, flagBytes.Length).SequenceEqual(flagBytes))
                        {
                            ulong _id = result.Buffer.AsSpan(flagBytes.Length).ToUInt64();
                            if (udptcss.TryRemove(_id, out TaskCompletionSource<IPEndPoint> _tcs))
                            {
                                _tcs.SetResult(result.RemoteEndPoint);
                            }
                            return;
                        }

                        int length = result.Buffer.Length;
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
                        result.Buffer.AsMemory().CopyTo(buffer);
                        _ = Task.Run(async () =>
                        {
                            //去连接
                            ulong id = ns.Increment();
                            try
                            {
                                if (await UdpConnect(token.ListenPort, id))
                                {
                                    TaskCompletionSource<IPEndPoint> tcs = new TaskCompletionSource<IPEndPoint>();
                                    udptcss.TryAdd(id, tcs);

                                    IPEndPoint remote = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                                    udpConnections.TryAdd(result.RemoteEndPoint, new UdpTargetCache { IPEndPoint = remote });

                                    await token.SourceSocket.SendAsync(buffer.AsMemory(0,length), remote);
                                }
                            }
                            catch (Exception)
                            {
                               
                            }
                            finally
                            {
                                udptcss.TryRemove(id, out _);
                                ArrayPool<byte>.Shared.Return(buffer);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
                if (udpListens.TryRemove(token.ListenPort, out token))
                {
                    token.Clear();
                }
            }
        }
        public async Task OnConnectUdp(ulong id, IPEndPoint server, IPEndPoint service)
        {
            Console.WriteLine($"{id}->{server}->{service}");

            UdpClient udpClient = new UdpClient();
            udpClient.Client.WindowsUdpBug();

            byte[] buffer = new byte[flagBytes.Length + 8];
            flagBytes.AsMemory().CopyTo(buffer);
            id.ToBytes(buffer.AsMemory(flagBytes.Length));

            await udpClient.SendAsync(buffer, server);

            UdpClient serviceUdpClient = null;
            while (true)
            {
                try
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync();

                    if (serviceUdpClient == null)
                    {
                        serviceUdpClient = new UdpClient();
                        serviceUdpClient.Client.WindowsUdpBug();
                        await serviceUdpClient.SendAsync(result.Buffer, service);
                        _ = Task.Run(async () =>
                        {
                            while (true)
                            {
                                try
                                {
                                    UdpReceiveResult result = await serviceUdpClient.ReceiveAsync();
                                    await udpClient.SendAsync(result.Buffer, server);
                                }
                                catch (Exception ex)
                                {
                                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                    {
                                        Logger.Instance.Error(ex);
                                    }

                                    udpClient?.Close();
                                    udpClient?.Dispose();

                                    serviceUdpClient?.Close();
                                    serviceUdpClient?.Dispose();

                                    break;
                                }
                            }
                        });
                    }
                    else
                    {
                        await serviceUdpClient.SendAsync(result.Buffer, service);
                    }
                }
                catch (Exception ex)
                {
                    if(Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }

                    udpClient?.Close();
                    udpClient?.Dispose();

                    serviceUdpClient?.Close();
                    serviceUdpClient?.Dispose();
                }
            }
        }


        private void UdpTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var connections = udpConnections.Where(c => c.Value.Timeout).Select(c => c.Key);
                    foreach (var item in connections)
                    {
                        udpConnections.TryRemove(item, out _);
                    }
                    await Task.Delay(5000);
                }
            });
        }
        private void CloseClientSocket(AsyncUserUdpToken token)
        {
            if (token == null) return;
            token.Clear();
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

    }

    public sealed class UdpTargetCache
    {
        public IPEndPoint IPEndPoint { get; set; }
        public long LastTime { get; set; } = Environment.TickCount64;
        public void Update()
        {
            LastTime = Environment.TickCount64;
        }
        public bool Timeout => Environment.TickCount64 - LastTime > 15000;
    }

    public sealed class AsyncUserUdpToken
    {
        public int ListenPort { get; set; }
        public UdpClient SourceSocket { get; set; }
        public UdpClient TargetSocket { get; set; }

        public IPEndPoint TempRemoteEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        public void Clear()
        {
            SourceSocket?.Close();
            SourceSocket = null;

            TargetSocket?.Close();
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
