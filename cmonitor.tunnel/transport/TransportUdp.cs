using cmonitor.tunnel.connection;
using common.libs;
using common.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.tunnel.transport
{
    public sealed class TransportUdp : ITunnelTransport
    {
        public string Name => "udp";

        public string Label => "udp";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;

        public Func<TunnelTransportInfo, Task<bool>> OnSendConnectBegin { get; set; } = async (info) => { return await Task.FromResult<bool>(false); };
        public Func<TunnelTransportInfo, Task> OnSendConnectFail { get; set; } = async (info) => { await Task.CompletedTask; };
        public Func<TunnelTransportInfo, Task> OnSendConnectSuccess { get; set; } = async (info) => { await Task.CompletedTask; };
        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.ttl");
        private byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.end");

        public TransportUdp()
        {
        }

        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                //正向连接
                if (await OnSendConnectBegin(tunnelTransportInfo) == false)
                {
                    return null;
                }
                await Task.Delay(500);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                _ = BindListen(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
                await Task.Delay(50);
                BindAndTTL(tunnelTransportInfo1);
                if (await OnSendConnectBegin(tunnelTransportInfo1) == false)
                {
                    return null;
                }
                ITunnelConnection connection = await WaitReverse(tunnelTransportInfo1);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo);
                    return connection;
                }
            }

            await OnSendConnectFail(tunnelTransportInfo);
            return null;
        }
        public void OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            Task.Run(async () =>
            {
                if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
                {
                    _ = BindListen(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
                    await Task.Delay(50);
                    BindAndTTL(tunnelTransportInfo);
                }
                else
                {

                    ITunnelConnection connection = await ConnectForward(tunnelTransportInfo);
                    if (connection != null)
                    {
                        OnConnected(connection);
                        await OnSendConnectSuccess(tunnelTransportInfo);
                    }
                    else
                    {
                        await OnSendConnectFail(tunnelTransportInfo);
                    }
                }
            });
        }


        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {
            //要连接哪些IP
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();

            //先尝试内网ipv4
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetwork))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //在尝试外网
            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //再尝试IPV6
            foreach (IPAddress item in localIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6))
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) == false)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .ToList();

            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", eps.Select(c => c.ToString()))}");
            }

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>();
            //接收远端数据，收到了就是成功了
            (UdpClient remoteUdp, UdpClient remoteUdp6) = BindListen(local, taskCompletionSource);

            foreach (IPEndPoint ep in eps.Where(c => NetworkHelper.NotIPv6Support(c.Address) == false))
            {
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    if (ep.AddressFamily == AddressFamily.InterNetwork)
                    {
                        remoteUdp.Send(authBytes, ep);
                    }
                    else
                    {
                        remoteUdp6.Send(authBytes, ep);
                    }
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex.Message);
                    }
                }
            }

            try
            {
                IPEndPoint remoteEP = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
                //绑定一个udp，用来给QUIC链接
                UdpClient localUdp = remoteEP.AddressFamily == AddressFamily.InterNetwork ? remoteUdp : remoteUdp6;
                if (remoteEP.AddressFamily == AddressFamily.InterNetwork)
                {
                    remoteUdp6.Close();
                    remoteUdp6.Dispose();
                }
                else
                {
                    remoteUdp.Close();
                    remoteUdp.Dispose();
                }

                return new TunnelConnectionUdp
                {
                    UdpClient = localUdp,
                    IPEndPoint = remoteEP,
                    TransactionId = tunnelTransportInfo.TransactionId,
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    TransportName = Name,
                    Direction = tunnelTransportInfo.Direction,
                    ProtocolType = ProtocolType,
                    Type = TunnelType.P2P,
                    Mode = TunnelMode.Client,
                    Label = string.Empty,
                };
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            try
            {
                remoteUdp.Close();
            }
            catch (Exception)
            {
            }
            return null;
        }
        private (UdpClient, UdpClient) BindListen(IPEndPoint local, TaskCompletionSource<IPEndPoint> tcs)
        {
            UdpClient udpClient = new UdpClient(local.AddressFamily);
            udpClient.Client.ReuseBind(local);
            udpClient.Client.WindowsUdpBug();
            IAsyncResult result = udpClient.BeginReceive((IAsyncResult result) =>
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = udpClient.EndReceive(result, ref ep);
                    udpClient.Send(endBytes, ep);
                    tcs.SetResult(ep);
                }
                catch (Exception)
                {
                }
            }, null);

            UdpClient udpClient6 = new UdpClient(AddressFamily.InterNetworkV6);
            udpClient6.Client.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
            udpClient6.Client.WindowsUdpBug();
            IAsyncResult result6 = udpClient6.BeginReceive((IAsyncResult result) =>
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = udpClient6.EndReceive(result, ref ep);
                    udpClient6.Send(endBytes, ep);
                    tcs.SetResult(ep);
                }
                catch (Exception)
                {
                }
            }, null);


            return (udpClient, udpClient6);
        }
        private async Task BindListen(IPEndPoint local, TunnelTransportInfo state)
        {
            UdpClient udpClient = new UdpClient(local.AddressFamily);
            UdpClient udpClient6 = new UdpClient(AddressFamily.InterNetworkV6);

            try
            {
                udpClient.Client.ReuseBind(local);
                udpClient.Client.WindowsUdpBug();
                ListenAsyncToken token = new ListenAsyncToken
                {
                    LocalUdp = udpClient,
                    Tcs = new TaskCompletionSource<AddressFamily>(),
                    State = state
                };
                _ = ListenReceiveCallback(token);


                udpClient6.Client.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
                udpClient6.Client.WindowsUdpBug();
                ListenAsyncToken token6 = new ListenAsyncToken
                {
                    LocalUdp = udpClient6,
                    Tcs = token.Tcs,
                    State = state
                };
                _ = ListenReceiveCallback(token6);

                AddressFamily af = await token.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(30000));

                if (af == AddressFamily.InterNetwork)
                {
                    udpClient6.Close();
                    udpClient6.Dispose();
                }
                else
                {
                    udpClient.Close();
                    udpClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                udpClient.Close();
                udpClient6.Close();
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
        }
        private async Task ListenReceiveCallback(ListenAsyncToken token)
        {
            try
            {
                while (true)
                {
                    UdpReceiveResult result = await token.LocalUdp.ReceiveAsync();
                    if (result.Buffer.Length == 0) break;

                    if (result.Buffer.Length == endBytes.Length && result.Buffer.AsSpan().SequenceEqual(endBytes))
                    {
                        if (token.Tcs != null && token.Tcs.Task.IsCompleted == false)
                        {
                            token.Tcs.SetResult(result.RemoteEndPoint.AddressFamily);
                            await OnUdpConnected(token.State, token.LocalUdp, result.RemoteEndPoint);
                        }
                        break;
                    }
                    else
                    {
                        token.LocalUdp.Send(result.Buffer, result.RemoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
            {
            }
        }
        private void BindAndTTL(TunnelTransportInfo tunnelTransportInfo)
        {
            //给对方发送TTL消息
            IPAddress[] localIps = tunnelTransportInfo.Remote.LocalIps.Where(c => c.Equals(tunnelTransportInfo.Remote.Local.Address) == false).ToArray();
            List<IPEndPoint> eps = new List<IPEndPoint>();
            foreach (IPAddress item in localIps)
            {
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Local.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port));
                eps.Add(new IPEndPoint(item, tunnelTransportInfo.Remote.Remote.Port + 1));
            }

            eps.AddRange(new List<IPEndPoint>{
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port),
                new IPEndPoint(tunnelTransportInfo.Remote.Remote.Address,tunnelTransportInfo.Remote.Remote.Port+1),
            });
            //本机有V6
            bool hasV6 = tunnelTransportInfo.Local.LocalIps.Any(c => c.AddressFamily == AddressFamily.InterNetworkV6);
            //本机的局域网ip和外网ip
            List<IPAddress> localLocalIps = tunnelTransportInfo.Local.LocalIps.Concat(new List<IPAddress> { tunnelTransportInfo.Local.Remote.Address }).ToList();
            eps = eps
                //对方是V6，本机也得有V6
                .Where(c => (c.AddressFamily == AddressFamily.InterNetworkV6 && hasV6) == false)
                //端口和本机端口一样，那不应该是换回地址
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && c.Address.Equals(IPAddress.Loopback)) == false)
                //端口和本机端口一样。那不应该是本机的IP
                .Where(c => (c.Port == tunnelTransportInfo.Local.Local.Port && localLocalIps.Any(d => d.Equals(c.Address))) == false)
                .ToList();

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            foreach (var ip in eps)
            {
                try
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ip}");
                    }

                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                        socket.WindowsUdpBug();
                        socket.ReuseBind(local);
                        socket.Ttl = (short)(tunnelTransportInfo.Local.RouteLevel);
                        _ = socket.SendToAsync(new byte[0], SocketFlags.None, ip);
                        socket.SafeClose();
                    }
                    else
                    {
                        Socket socket = new Socket(ip.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                        socket.WindowsUdpBug();
                        socket.ReuseBind(new IPEndPoint(IPAddress.IPv6Any, local.Port));
                        socket.Ttl = 2;
                        _ = socket.SendToAsync(new byte[0], SocketFlags.None, ip);
                        socket.SafeClose();
                    }
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex.Message);
                    }
                }
                finally
                {
                }
            }
        }

        private ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>> reverseDic = new ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>>();
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>();
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000));
                return connection;
            }
            catch (Exception)
            {
            }
            finally
            {
                reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
            }
            return null;
        }
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.SetResult(null);
            }
        }

        private async Task OnUdpConnected(object _state, UdpClient localUdp, IPEndPoint remoteEP)
        {
            TunnelTransportInfo state = _state as TunnelTransportInfo;
            if (state.TransportName == Name)
            {
                try
                {
                    TunnelConnectionUdp result = new TunnelConnectionUdp
                    {
                        UdpClient = localUdp,
                        RemoteMachineId = state.Remote.MachineId,
                        RemoteMachineName = state.Remote.MachineName,
                        Direction = state.Direction,
                        ProtocolType = TunnelProtocolType.Udp,
                        Type = TunnelType.P2P,
                        Mode = TunnelMode.Server,
                        TransactionId = state.TransactionId,
                        TransportName = state.TransportName,
                        IPEndPoint = remoteEP,
                        Label = string.Empty,
                    };
                    if (reverseDic.TryRemove(state.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
                    {
                        tcs.SetResult(result);
                        return;
                    }
                    OnConnected(result);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            }
            await Task.CompletedTask;
        }

        sealed class ListenAsyncToken
        {
            public UdpClient LocalUdp { get; set; }
            public TaskCompletionSource<AddressFamily> Tcs { get; set; }
            public object State { get; set; }
        }

    }


}
