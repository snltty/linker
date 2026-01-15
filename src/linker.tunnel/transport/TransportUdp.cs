using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using linker.tunnel.wanport;
using System.Security.Cryptography.X509Certificates;
using linker.libs.timer;
using System.Buffers;

namespace linker.tunnel.transport
{
    /// <summary>
    ///  UDP 打洞
    ///  
    ///  大致原理（正向打洞）
    ///  A 通知 B，我要连你
    ///  B 收到通知，开始监听收取消息，并且给A随便发送个消息，这时候A肯定收不到
    ///  A 监听收取消息，并且给 B 发送的试探消息，如果B能收到消息，就会回复一条消息
    ///  A 能收到回复，就说明通了，这隧道能用
    /// 
    /// 反向打洞几乎同理，逻辑变通一下即可
    /// </summary>
    public sealed class TransportUdp : ITunnelTransport
    {
        public string Name => "Udp";

        public string Label => "UDP、非常纯";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Udp;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 3;


        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private readonly byte[] authBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ttl1");
        private readonly byte[] endBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.end1");

        private readonly ITunnelMessengerAdapter tunnelMessengerAdapter;
        public TransportUdp(ITunnelMessengerAdapter tunnelMessengerAdapter)
        {
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }
        public void SetSSL(X509Certificate certificate)
        {
        }

        /// <summary>
        /// 开始连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                //正向连接
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    return null;
                }
                await Task.Delay(1000).ConfigureAwait(false);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                _ = BindListen(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
                BindAndTTL(tunnelTransportInfo1);
                await Task.Delay(1000).ConfigureAwait(false);
                if (await tunnelMessengerAdapter.SendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
                ITunnelConnection connection = await WaitReverse(tunnelTransportInfo1).ConfigureAwait(false);
                if (connection != null)
                {
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }

            await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// 收到连接请求
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            //他要连我
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                //我监听连接
                _ = BindListen(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
                await Task.Delay(50).ConfigureAwait(false);
                //给它随便发送一些消息，然后他就可以来连我了
                BindAndTTL(tunnelTransportInfo);
            }
            else
            {
                //我去连接他
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection);
                    await tunnelMessengerAdapter.SendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                }
                else
                {
                    await tunnelMessengerAdapter.SendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// 连接对方
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
            //监听连接
            Socket remoteUdp = BindListen(tunnelTransportInfo.Local.Local, taskCompletionSource, tunnelTransportInfo.RemoteEndPoints.Select(c => c.Address).ToList());

            //给对方发送简单消息
            foreach (IPEndPoint ep in tunnelTransportInfo.RemoteEndPoints)
            {
                try
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ep}");
                    }
                    remoteUdp.SendTo(authBytes, ep);
                    await Task.Delay(50).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }

            try
            {
                //然后等待对方回复，如果能收到回复，就说明是通了
                IPEndPoint remoteEP = await taskCompletionSource.WithTimeout(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                return new TunnelConnectionUdp
                {
                    UdpClient = remoteUdp,
                    IPEndPoint = NetworkHelper.TransEndpointFamily(remoteEP),
                    TransactionId = tunnelTransportInfo.TransactionId,
                    TransactionTag = tunnelTransportInfo.TransactionTag,
                    RemoteMachineId = tunnelTransportInfo.Remote.MachineId,
                    RemoteMachineName = tunnelTransportInfo.Remote.MachineName,
                    TransportName = Name,
                    Direction = tunnelTransportInfo.Direction,
                    ProtocolType = ProtocolType,
                    Type = TunnelType.P2P,
                    Mode = TunnelMode.Client,
                    Label = string.Empty,
                    Receive = true,
                    SSL = tunnelTransportInfo.SSL,
                    Crypto = CryptoFactory.CreateSymmetric(tunnelTransportInfo.Remote.MachineId)
                };
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetResult(null);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
               
            }
            remoteUdp?.SafeClose();
            return null;
        }

        /// <summary>
        /// 开启一个监听，然后等待对方发来消息
        /// </summary>
        /// <param name="local"></param>
        /// <param name="tcs"></param>
        /// <returns></returns>
        private Socket BindListen(IPEndPoint local, TaskCompletionSource<IPEndPoint> tcs, List<IPAddress> ips)
        {
            local = new IPEndPoint(IPAddress.IPv6Any, local.Port);
            Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            socket.IPv6Only(local.AddressFamily, false);
            socket.WindowsUdpBug();
            socket.ReuseBind(local);

            TimerHelper.Async(async () =>
            {
                using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024);
                SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer.Memory, new IPEndPoint(IPAddress.IPv6Any, 0)).ConfigureAwait(false);
                await socket.SendToAsync(endBytes, result.RemoteEndPoint).ConfigureAwait(false);
                tcs.TrySetResult(result.RemoteEndPoint as IPEndPoint);
            });
            return socket;
        }

        /// <summary>
        /// 开启一个监听，等待对方发来消息，如果收到消息，就给对方回复消息，对方就知道通了
        /// </summary>
        /// <param name="local"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private async Task BindListen(IPEndPoint local, TunnelTransportInfo state)
        {
            local = new IPEndPoint(IPAddress.IPv6Any, local.Port);
            Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                socket.IPv6Only(local.AddressFamily, false);
                socket.ReuseBind(local);
                socket.WindowsUdpBug();
                ListenAsyncToken token = new ListenAsyncToken
                {
                    LocalUdp = socket,
                    Tcs = new TaskCompletionSource<AddressFamily>(TaskCreationOptions.RunContinuationsAsynchronously),
                    State = state
                };
                _ = ListenReceiveCallback(token);

                try
                {
                    AddressFamily af = await token.Tcs.WithTimeout(TimeSpan.FromMilliseconds(30000)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    socket.SafeClose();
                    token.Tcs.TrySetResult(AddressFamily.InterNetwork);
                }
                return;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            socket.SafeClose();
        }
        private async Task ListenReceiveCallback(ListenAsyncToken token)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
                while (token.Tcs.Task.IsCompleted == false)
                {
                    SocketReceiveFromResult result = await token.LocalUdp.ReceiveFromAsync(buffer.Memory, ep).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        token.Tcs.TrySetResult(result.RemoteEndPoint.AddressFamily);
                        break;
                    }
                    if (result.ReceivedBytes == endBytes.Length && buffer.Memory.Span.Slice(0, result.ReceivedBytes).SequenceEqual(endBytes))
                    {
                        if (token.Tcs != null && token.Tcs.Task.IsCompleted == false)
                        {
                            token.Tcs.TrySetResult(result.RemoteEndPoint.AddressFamily);
                            await OnUdpConnected(token.State, token.LocalUdp, result.RemoteEndPoint as IPEndPoint).ConfigureAwait(false);
                        }
                        break;
                    }
                    else
                    {
                        await token.LocalUdp.SendToAsync(buffer.Memory.Slice(0, result.ReceivedBytes), result.RemoteEndPoint).ConfigureAwait(false);
                    }
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
            }
        }

        /// <summary>
        /// 随便发送一些消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        private void BindAndTTL(TunnelTransportInfo tunnelTransportInfo)
        {
            IPEndPoint local = new IPEndPoint(IPAddress.IPv6Any, tunnelTransportInfo.Local.Local.Port);
            foreach (var ip in tunnelTransportInfo.RemoteEndPoints)
            {
                try
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ip}");
                    }

                    Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                    socket.IPv6Only(local.AddressFamily, false);
                    socket.WindowsUdpBug();
                    socket.ReuseBind(local);
                    socket.Ttl = (short)(tunnelTransportInfo.Local.RouteLevel);
                    _ = socket.SendToAsync(authBytes, SocketFlags.None, ip);
                    socket.SafeClose();
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
                }
            }
        }

        private ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>> reverseDic = new ConcurrentDictionary<string, TaskCompletionSource<ITunnelConnection>>();
        /// <summary>
        /// 反向连接的时候，等待对方来连
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        /// <returns></returns>
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.WithTimeout(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
                return connection;
            }
            catch (Exception)
            {
                tcs.TrySetResult(null);
            }
            finally
            {
                reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out _);
            }
            return null;
        }

        /// <summary>
        /// 收到打洞失败消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.TrySetResult(null);
            }
        }
        /// <summary>
        /// 收到打洞成功消息
        /// </summary>
        /// <param name="tunnelTransportInfo"></param>
        public void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
            if (reverseDic.TryRemove(tunnelTransportInfo.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
            {
                tcs.TrySetResult(null);
            }
        }

        private async Task OnUdpConnected(object _state, Socket localUdp, IPEndPoint remoteEP)
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
                        TransactionTag = state.TransactionTag,
                        TransportName = state.TransportName,
                        IPEndPoint = NetworkHelper.TransEndpointFamily(remoteEP),
                        Label = string.Empty,
                        Receive = true,
                        SSL = state.SSL,
                        Crypto = CryptoFactory.CreateSymmetric(state.Local.MachineId)
                    };
                    if (reverseDic.TryRemove(state.Remote.MachineId, out TaskCompletionSource<ITunnelConnection> tcs))
                    {
                        tcs.TrySetResult(result);
                        return;
                    }
                    OnConnected(result);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        sealed class ListenAsyncToken
        {
            public Socket LocalUdp { get; set; }
            public TaskCompletionSource<AddressFamily> Tcs { get; set; }
            public object State { get; set; }
        }

    }


}
