using linker.libs;
using linker.libs.extends;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace linker.plugins.sforward.proxy
{
    public partial class SForwardProxy
    {
        private ConcurrentDictionary<int, AsyncUserToken> tcpListens = new ConcurrentDictionary<int, AsyncUserToken>();
        private ConcurrentDictionary<ulong, TaskCompletionSource<Socket>> tcpConnections = new ConcurrentDictionary<ulong, TaskCompletionSource<Socket>>();


        public Func<int, ulong, Task<bool>> TunnelConnect { get; set; } = async (port, id) => { return await Task.FromResult(false); };
        public Func<string,int, ulong, Task<bool>> WebConnect { get; set; } = async (host,port, id) => { return await Task.FromResult(false); };


        private void StartTcp(int port, bool isweb)
        {
            IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.ReuseBind(localEndPoint);
            socket.Listen(int.MaxValue);
            AsyncUserToken userToken = new AsyncUserToken
            {
                ListenPort = port,
                SourceSocket = socket,
                IsWeb = isweb,
            };
            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = userToken,
                SocketFlags = SocketFlags.None,
            };

            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            tcpListens.AddOrUpdate(port, userToken, (a, b) => userToken);
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
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
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
                default:
                    break;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.AcceptSocket != null)
                {
                    AsyncUserToken acceptToken = (AsyncUserToken)e.UserToken;
                    Socket socket = e.AcceptSocket;
                    if (socket != null && socket.RemoteEndPoint != null)
                    {
                        socket.KeepAlive();
                        AsyncUserToken userToken = new AsyncUserToken
                        {
                            SourceSocket = socket,
                            ListenPort = acceptToken.ListenPort,
                            IsWeb = acceptToken.IsWeb,
                        };
                        _ = BindReceive(userToken);
                    }
                    StartAccept(e);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private async Task BindReceive(AsyncUserToken token)
        {
            ulong id = ns.Increment();
            byte[] buffer1 = ArrayPool<byte>.Shared.Rent(8 * 1024);
            byte[] buffer2 = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int length = await token.SourceSocket.ReceiveAsync(buffer1.AsMemory(), SocketFlags.None);
                if (length > flagBytes.Length && buffer1.AsSpan(0, flagBytes.Length).SequenceEqual(flagBytes))
                {
                    ulong _id = buffer1.AsSpan(flagBytes.Length).ToUInt64();
                    if (tcpConnections.TryRemove(_id, out TaskCompletionSource<Socket> _tcs))
                    {
                        _tcs.SetResult(token.SourceSocket);
                    }
                    return;
                }

                if (token.IsWeb)
                {

                    token.Host = GetHost(buffer1.AsMemory(0, length));
                    if (string.IsNullOrWhiteSpace(token.Host))
                    {
                        CloseClientSocket(token);
                        return;
                    }
                    if (await WebConnect(token.Host, token.ListenPort, id) == false)
                    {
                        CloseClientSocket(token);
                        return;
                    }
                }
                else
                {
                    if(await TunnelConnect(token.ListenPort, id) == false)
                    {
                        CloseClientSocket(token);
                        return;
                    }
                }

                TaskCompletionSource<Socket> tcs = new TaskCompletionSource<Socket>();
                tcpConnections.TryAdd(id, tcs);
                token.TargetSocket = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(2000)).ConfigureAwait(false);

                await token.TargetSocket.SendAsync(buffer1.AsMemory(0, length));
                await Task.WhenAll(SwarpData(token, buffer1, token.SourceSocket, token.TargetSocket), SwarpData(token,buffer2, token.TargetSocket, token.SourceSocket));

                CloseClientSocket(token);
            }
            catch (Exception)
            {
                CloseClientSocket(token);
            }
            finally
            {
                tcpConnections.TryRemove(id, out _);
                ArrayPool<byte>.Shared.Return(buffer1);
                ArrayPool<byte>.Shared.Return(buffer2);
            }
        }
        public async Task OnConnectTcp(ulong id, IPEndPoint server, IPEndPoint service)
        {
            Socket sourceSocket = null;
            Socket targetSocket = null;
            byte[] buffer1 = ArrayPool<byte>.Shared.Rent(8 * 1024);
            byte[] buffer2 = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                sourceSocket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sourceSocket.IPv6Only(server.AddressFamily, false);
                await sourceSocket.ConnectAsync(server);

                targetSocket = new Socket(service.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.IPv6Only(service.AddressFamily, false);
                await targetSocket.ConnectAsync(service);

                flagBytes.AsMemory().CopyTo(buffer1);
                id.ToBytes(buffer1.AsMemory(flagBytes.Length));
                await sourceSocket.SendAsync(buffer1.AsMemory(0, flagBytes.Length + 8));

                await Task.WhenAll(SwarpData(null,buffer1, sourceSocket, targetSocket), SwarpData(null,buffer2, targetSocket, sourceSocket));

            }
            catch (Exception)
            {
                sourceSocket?.SafeClose();
                targetSocket?.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer1);
                ArrayPool<byte>.Shared.Return(buffer2);
            }
        }

        private byte[] hostBytes = Encoding.UTF8.GetBytes("Host: ");
        private byte[] wrapBytes = Encoding.UTF8.GetBytes("\r\n");
        private byte[] colonBytes = Encoding.UTF8.GetBytes(":");
        private string GetHost(Memory<byte> buffer)
        {
            int start = buffer.Span.IndexOf(hostBytes);
            if (start < 0) return string.Empty;
            start += hostBytes.Length;

            int length = buffer.Span.Slice(start).IndexOf(wrapBytes);

            int length1 = buffer.Span.Slice(start, length).IndexOf(colonBytes);
            if (length1 > 0) length = length1;

            return Encoding.UTF8.GetString(buffer.Slice(start, length).Span);
        }
        private async Task SwarpData(AsyncUserToken token,Memory<byte> buffer, Socket source, Socket target)
        {
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReceiveAsync(buffer).ConfigureAwait(false)) != 0)
                {
                    await target.SendAsync(buffer.Slice(0, bytesRead)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
        }

        private void CloseClientSocket(AsyncUserToken token)
        {
            if (token == null) return;
            token.Clear();
        }
        public void StopTcp()
        {
            foreach (var item in tcpListens)
            {
                CloseClientSocket(item.Value);
            }
            tcpListens.Clear();
        }
        public void StopTcp(int port)
        {
            if (tcpListens.TryRemove(port, out AsyncUserToken userToken))
            {
                CloseClientSocket(userToken);
            }
        }

    }

    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }
        public string Host { get; set; }
        public bool IsWeb { get; set; }
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }

        public void Clear()
        {
            SourceSocket?.SafeClose();
            TargetSocket?.SafeClose();

            GC.Collect();
        }
    }
}
