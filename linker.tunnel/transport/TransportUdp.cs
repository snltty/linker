using linker.tunnel.connection;
using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using linker.tunnel.wanport;

namespace linker.tunnel.transport
{
    public sealed class TransportUdp : ITunnelTransport
    {
        public string Name => "udp";

        public string Label => "UDP、非常纯";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Udp;
        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Udp;
        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 3;


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
                if (await OnSendConnectBegin(tunnelTransportInfo).ConfigureAwait(false) == false)
                {
                    return null;
                }
                await Task.Delay(500).ConfigureAwait(false);
                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }
            else if (tunnelTransportInfo.Direction == TunnelDirection.Reverse)
            {
                //反向连接
                TunnelTransportInfo tunnelTransportInfo1 = tunnelTransportInfo.ToJsonFormat().DeJson<TunnelTransportInfo>();
                _ = BindListen(tunnelTransportInfo1.Local.Local, tunnelTransportInfo1);
                await Task.Delay(50).ConfigureAwait(false);
                BindAndTTL(tunnelTransportInfo1);
                if (await OnSendConnectBegin(tunnelTransportInfo1).ConfigureAwait(false) == false)
                {
                    return null;
                }
                ITunnelConnection connection = await WaitReverse(tunnelTransportInfo1).ConfigureAwait(false);
                if (connection != null)
                {
                    await OnSendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                    return connection;
                }
            }

            await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
            return null;
        }
        public async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {
            if (tunnelTransportInfo.Direction == TunnelDirection.Forward)
            {
                _ = BindListen(tunnelTransportInfo.Local.Local, tunnelTransportInfo);
                await Task.Delay(50).ConfigureAwait(false);
                BindAndTTL(tunnelTransportInfo);
            }
            else
            {

                ITunnelConnection connection = await ConnectForward(tunnelTransportInfo).ConfigureAwait(false);
                if (connection != null)
                {
                    OnConnected(connection);
                    await OnSendConnectSuccess(tunnelTransportInfo).ConfigureAwait(false);
                }
                else
                {
                    await OnSendConnectFail(tunnelTransportInfo).ConfigureAwait(false);
                }
            }
        }


        private async Task<ITunnelConnection> ConnectForward(TunnelTransportInfo tunnelTransportInfo)
        {

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Warning($"{Name} connect to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {string.Join("\r\n", tunnelTransportInfo.RemoteEndPoints.Select(c => c.ToString()))}");
            }

            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            TaskCompletionSource<IPEndPoint> taskCompletionSource = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
            //接收远端数据，收到了就是成功了
            Socket remoteUdp = BindListen(local, taskCompletionSource);

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
                IPEndPoint remoteEP = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                return new TunnelConnectionUdp
                {
                    UdpClient = remoteUdp,
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
                    Receive = true,
                    SSL = tunnelTransportInfo.SSL,
                    Crypto = CryptoFactory.CreateSymmetric(tunnelTransportInfo.Remote.MachineId)
                };
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                remoteUdp?.SafeClose();
            }

            return null;
        }
        private Socket BindListen(IPEndPoint local, TaskCompletionSource<IPEndPoint> tcs)
        {
            Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            socket.WindowsUdpBug();
            socket.ReuseBind(local);

            TimerHelper.Async(async () =>
            {
                byte[] buffer = new byte[1024];
                SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, new IPEndPoint(IPAddress.Any, 0)).ConfigureAwait(false);
                await socket.SendToAsync(endBytes, result.RemoteEndPoint);
                tcs.SetResult(result.RemoteEndPoint as IPEndPoint);
            });
            return socket;
        }
        private async Task BindListen(IPEndPoint local, TunnelTransportInfo state)
        {
            Socket socket = new Socket(local.AddressFamily, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                socket.ReuseBind(local);
                socket.WindowsUdpBug();
                ListenAsyncToken token = new ListenAsyncToken
                {
                    LocalUdp = socket,
                    Tcs = new TaskCompletionSource<AddressFamily>(TaskCreationOptions.RunContinuationsAsynchronously),
                    State = state
                };
                _ = ListenReceiveCallback(token);

                AddressFamily af = await token.Tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(30000)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                socket.SafeClose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }
        private async Task ListenReceiveCallback(ListenAsyncToken token)
        {
            try
            {
                byte[] buffer = new byte[8 * 1024];
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    SocketReceiveFromResult result = await token.LocalUdp.ReceiveFromAsync(buffer, ep).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0) break;

                    if (result.ReceivedBytes == endBytes.Length && buffer.AsSpan(0, result.ReceivedBytes).SequenceEqual(endBytes))
                    {
                        if (token.Tcs != null && token.Tcs.Task.IsCompleted == false)
                        {
                            token.Tcs.SetResult(result.RemoteEndPoint.AddressFamily);
                            await OnUdpConnected(token.State, token.LocalUdp, result.RemoteEndPoint as IPEndPoint).ConfigureAwait(false);
                        }
                        break;
                    }
                    else
                    {
                        await token.LocalUdp.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), result.RemoteEndPoint);
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
        private void BindAndTTL(TunnelTransportInfo tunnelTransportInfo)
        {
            IPEndPoint local = new IPEndPoint(tunnelTransportInfo.Local.Local.Address, tunnelTransportInfo.Local.Local.Port);
            foreach (var ip in tunnelTransportInfo.RemoteEndPoints)
            {
                try
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Warning($"{Name} ttl to {tunnelTransportInfo.Remote.MachineId}->{tunnelTransportInfo.Remote.MachineName} {ip}");
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
        private async Task<ITunnelConnection> WaitReverse(TunnelTransportInfo tunnelTransportInfo)
        {
            TaskCompletionSource<ITunnelConnection> tcs = new TaskCompletionSource<ITunnelConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            reverseDic.TryAdd(tunnelTransportInfo.Remote.MachineId, tcs);

            try
            {
                ITunnelConnection connection = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);
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
                        TransportName = state.TransportName,
                        IPEndPoint = remoteEP,
                        Label = string.Empty,
                        Receive = true,
                        SSL = state.SSL,
                        Crypto = CryptoFactory.CreateSymmetric(state.Local.MachineId)
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
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }
            await Task.CompletedTask;
        }

        sealed class ListenAsyncToken
        {
            public Socket LocalUdp { get; set; }
            public TaskCompletionSource<AddressFamily> Tcs { get; set; }
            public object State { get; set; }
        }

    }


}
