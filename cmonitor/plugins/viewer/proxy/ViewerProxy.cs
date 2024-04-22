using common.libs;
using common.libs.extends;
using MemoryPack;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cmonitor.plugins.viewer.proxy
{
    public class ViewerProxy
    {
        private readonly WheelTimer<ConnectServerCache> wheelTimer = new WheelTimer<ConnectServerCache>();
        private readonly ConcurrentDictionary<uint, ConnectServerCache> connects = new ConcurrentDictionary<uint, ConnectServerCache>();
        private NumberSpaceUInt32 ns = new NumberSpaceUInt32();
        private SocketAsyncEventArgs acceptEventArg;

        private Socket socket;

        public IPEndPoint LocalEndpoint => socket?.LocalEndPoint as IPEndPoint ?? new IPEndPoint(IPAddress.Any, 0);

        public ViewerProxy()
        {
        }

        public void Start(int port)
        {
            try
            {
                Stop();

                IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
                socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.IPv6Only(localEndPoint.AddressFamily, false);
                socket.ReuseBind(localEndPoint);
                socket.Listen(int.MaxValue);

                acceptEventArg = new SocketAsyncEventArgs
                {
                    UserToken = new AsyncUserToken
                    {
                        SourceSocket = socket
                    },
                    SocketFlags = SocketFlags.None,
                };

                acceptEventArg.Completed += IO_Completed;
                StartAccept(acceptEventArg);

            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            AsyncUserToken token = (AsyncUserToken)acceptEventArg.UserToken;
            try
            {
                if (token.SourceSocket.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
                token.Clear();
            }
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket != null)
            {
                BindReceive(e);
                StartAccept(e);
            }
        }
        private void BindReceive(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                var socket = e.AcceptSocket;

                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return;
                }

                socket.KeepAlive();
                AsyncUserToken userToken = new AsyncUserToken
                {
                    SourceSocket = socket
                };

                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                readEventArgs.SetBuffer(new byte[8 * 1024], 0, 8 * 1024);
                readEventArgs.Completed += IO_Completed;
                if (socket.ReceiveAsync(readEventArgs) == false)
                {
                    ProcessReceive(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
        }
        private async void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;
                    if (token.Step == ViewerProxyStep.Request)
                    {
                        await ReadPacket(e, token, e.Buffer.AsMemory(0, length));
                        return;
                    }
                    await token.TargetSocket.SendAsync(e.Buffer.AsMemory(0, length), SocketFlags.None);
                    if (token.SourceSocket.Available > 0)
                    {
                        while (token.SourceSocket.Available > 0)
                        {
                            length = token.SourceSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await token.TargetSocket.SendAsync(e.Buffer.AsMemory(0, length), SocketFlags.None);
                            }
                            else
                            {
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (token.SourceSocket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }

                    if (token.SourceSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);

                CloseClientSocket(e);
            }
        }
        private async Task ReadPacket(SocketAsyncEventArgs e, AsyncUserToken token, Memory<byte> data)
        {
            if (GetMachineName(data, out string machine))
            {
                uint id = ns.Increment();
                byte[] tempData = new byte[data.Length];
                data.CopyTo(tempData);
                ConnectServerCache cache = new ConnectServerCache { Id = id, Saea = e, Data = tempData };
                connects.TryAdd(id, cache);

                WheelTimerTimeout<ConnectServerCache> timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<ConnectServerCache> { State = cache, Callback = ConnectTimeout, }, 3000);
                cache.Timeout = timeout;

                await Connect(machine, cache.Id);
            }
            else if (GetConnectId(data, out uint connectId))
            {
                if (connects.TryRemove(connectId, out ConnectServerCache cache))
                {
                    cache.Timeout.Cancel();
                    AsyncUserToken sourceToken = cache.Saea.UserToken as AsyncUserToken;
                    sourceToken.Step = ViewerProxyStep.Forward;
                    sourceToken.TargetSocket = token.SourceSocket;
                    sourceToken.TargetSocket.KeepAlive();

                    await sourceToken.TargetSocket.SendAsync(cache.Data, SocketFlags.None);

                    cache.Clear();

                    if (sourceToken.SourceSocket.ReceiveAsync(cache.Saea) == false)
                    {
                        ProcessReceive(cache.Saea);
                    }
                    BindReceiveTarget(sourceToken);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void ConnectTimeout(WheelTimerTimeout<ConnectServerCache> timeout)
        {
            if (timeout.IsCanceled == false)
            {
                if (connects.TryRemove(timeout.Task.State.Id, out ConnectServerCache cache))
                {
                    CloseClientSocket(cache.Saea);
                    cache.Clear();
                }
            }
        }

        public virtual async Task Connect(string name, uint connectId)
        {
            await Task.CompletedTask;
        }
        public async Task<bool> Connect(ViewerProxyInfo viewerProxyInfo)
        {
            Socket proxySocket = null;
            Socket targetSocket = null;
            try
            {
                proxySocket = new Socket(viewerProxyInfo.ProxyEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                proxySocket.KeepAlive();
                await proxySocket.ConnectAsync(viewerProxyInfo.ProxyEP);

                targetSocket = new Socket(viewerProxyInfo.TargetEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.KeepAlive();
                await targetSocket.ConnectAsync(viewerProxyInfo.TargetEP);

                int length = responseBytes.Length + 4;
                byte[] data = ArrayPool<byte>.Shared.Rent(length);
                responseBytes.AsMemory().CopyTo(data);
                viewerProxyInfo.ConnectId.ToBytes(data.AsMemory(responseBytes.Length));
                await proxySocket.SendAsync(data.AsMemory(0, length));
                ArrayPool<byte>.Shared.Return(data);

                BindReceiveTarget(new AsyncUserToken { SourceSocket = proxySocket, TargetSocket = targetSocket });
                BindReceiveTarget(new AsyncUserToken { SourceSocket = targetSocket, TargetSocket = proxySocket });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"proxy ep:{viewerProxyInfo.ProxyEP}");
                Logger.Instance.Error($"target ep:{viewerProxyInfo.TargetEP}");
                Logger.Instance.Error(ex);
                proxySocket?.SafeClose();
                targetSocket?.SafeClose();
            }
            return false;
        }


        private void IO_CompletedTarget(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceiveTarget(e);
                    break;
                default:
                    break;
            }
        }
        private void BindReceiveTarget(AsyncUserToken userToken)
        {
            try
            {
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = userToken,
                    SocketFlags = SocketFlags.None,
                };
                readEventArgs.SetBuffer(new byte[8 * 1024], 0, 8 * 1024);
                readEventArgs.Completed += IO_CompletedTarget;
                if (userToken.TargetSocket.ReceiveAsync(readEventArgs) == false)
                {
                    ProcessReceiveTarget(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
        }
        private async void ProcessReceiveTarget(SocketAsyncEventArgs e)
        {
            try
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;

                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;

                    await token.SourceSocket.SendAsync(e.Buffer.AsMemory(0, length), SocketFlags.None);

                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await token.SourceSocket.SendAsync(e.Buffer.AsMemory(0, length), SocketFlags.None);
                            }
                            else
                            {
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (token.TargetSocket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }

                    if (token.TargetSocket.ReceiveAsync(e) == false)
                    {
                        ProcessReceiveTarget(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);

                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (e == null) return;
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.SourceSocket != null)
            {
                token.Clear();
                e.Dispose();
            }
        }
        public void Stop()
        {
            CloseClientSocket(acceptEventArg);
        }

        private readonly byte[] endBytes = Encoding.UTF8.GetBytes("\r\n");
        private readonly byte[] startBytes = Encoding.UTF8.GetBytes("mstshash=");
        private byte[] responseBytes = Encoding.UTF8.GetBytes("snltty=");
        private bool GetMachineName(Memory<byte> memory, out string machine)
        {
            machine = string.Empty;

            int start = memory.Span.IndexOf(startBytes);
            if (start < 0) return false;

            memory = memory.Slice(start);
            int end = memory.Span.IndexOf(endBytes);
            if (end < 0) return false;

            machine = Encoding.UTF8.GetString(memory.Span.Slice(startBytes.Length, end - startBytes.Length));

            return true;
        }
        private bool GetConnectId(Memory<byte> memory, out uint id)
        {
            var span = memory.Span;
            id = 0;
            if (span.Length != responseBytes.Length + 4 || span.Slice(0, responseBytes.Length).SequenceEqual(responseBytes) == false)
            {
                return false;
            }
            id = span.Slice(responseBytes.Length).ToUInt32();
            return true;
        }


    }

    [MemoryPackable]
    public sealed partial class ViewerProxyInfo
    {
        public uint ConnectId { get; set; }

        public string ViewerServerMachine { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint ProxyEP { get; set; }

        [MemoryPackAllowSerialize]
        public IPEndPoint TargetEP { get; set; }
    }

    public sealed class ConnectServerCache
    {
        public uint Id { get; set; }
        public SocketAsyncEventArgs Saea { get; set; }
        public WheelTimerTimeout<ConnectServerCache> Timeout { get; set; }

        public Memory<byte> Data { get; set; }

        public void Clear()
        {
        }


    }

    public sealed class AsyncUserToken
    {
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }
        public ViewerProxyStep Step { get; set; } = ViewerProxyStep.Request;

        public void Clear()
        {
            SourceSocket?.SafeClose();
            SourceSocket = null;

            TargetSocket?.SafeClose();
            TargetSocket = null;

            GC.Collect();
        }
    }

    public enum ViewerProxyStep : byte
    {
        Request = 1,
        Forward = 2
    }
}
