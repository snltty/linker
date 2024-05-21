using common.libs;
using common.libs.extends;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace cmonitor.client.tunnel
{
    public delegate Task TunnelReceivceCallback(ITunnelConnection connection, Memory<byte> data, object state);
    public delegate Task TunnelCloseCallback(ITunnelConnection connection, object state);

    public enum TunnelProtocolType : byte
    {
        Tcp = ProtocolType.Tcp,
        Udp = ProtocolType.Udp,
    }
    public enum TunnelType : byte
    {
        P2P = 0,
        Relay = 1,
    }
    public enum TunnelDirection : byte
    {
        Forward = 0,
        Reverse = 1
    }

    public interface ITunnelConnectionCallback
    {
        public Task Receive(ITunnelConnection connection, Memory<byte> data, object state);
        public Task Closed(ITunnelConnection connection, object state);
    }

    public interface ITunnelConnection
    {
        public string RemoteMachineName { get; }
        public string TransactionId { get; }
        public string TransportName { get; }
        public string Label { get; }
        public TunnelType Type { get; }
        public TunnelProtocolType ProtocolType { get; }
        public TunnelDirection Direction { get; }
        public IPEndPoint IPEndPoint { get; }

        public bool Connected { get; }

        public Task SendAsync(Memory<byte> data, CancellationToken cancellationToken = default);
        public void BeginReceive(ITunnelConnectionCallback callback, object userToken);

        public void Close();

        public string ToString();
    }

    public sealed class TunnelConnectionTcp : ITunnelConnection
    {
        public string RemoteMachineName { get; init; }

        public string TransactionId { get; init; }

        public string TransportName { get; init; }

        public string Label { get; init; }

        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public TunnelDirection Direction { get; init; }

        public IPEndPoint IPEndPoint => (Socket?.RemoteEndPoint ?? new IPEndPoint(IPAddress.Any, 0)) as IPEndPoint;

        public bool Connected => Socket != null && Socket.Connected;

        [JsonIgnore]
        public Socket Socket { get; init; }


        private ITunnelConnectionCallback callback;

        public void BeginReceive(ITunnelConnectionCallback callback, object userToken)
        {
            if (this.callback != null) return;

            this.callback = callback;
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = userToken,
                SocketFlags = SocketFlags.None,
            };
            readEventArgs.SetBuffer(new byte[8 * 1024], 0, 8 * 1024);
            readEventArgs.Completed += IO_Completed;
            if (Socket.ReceiveAsync(readEventArgs) == false)
            {
                ProcessReceiveTarget(readEventArgs);
            }
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
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
        private async void ProcessReceiveTarget(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    int offset = e.Offset;
                    int length = e.BytesTransferred;

                    try
                    {
                        await callback.Receive(this, e.Buffer.AsMemory(offset, length), e.UserToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                    }

                    if (Socket.Available > 0)
                    {
                        while (Socket.Available > 0)
                        {
                            length = await Socket.ReceiveAsync(e.Buffer.AsMemory(), SocketFlags.None);
                            if (length > 0)
                            {
                                try
                                {
                                    await callback.Receive(this, e.Buffer.AsMemory(offset, length), e.UserToken).ConfigureAwait(false);
                                }
                                catch (Exception)
                                {
                                }
                            }
                            else
                            {
                                CloseClientSocket(e);
                                return;
                            }
                        }
                    }

                    if (Socket.Connected == false)
                    {
                        CloseClientSocket(e);
                        return;
                    }

                    if (Socket.ReceiveAsync(e) == false)
                    {
                        ProcessReceiveTarget(e);
                    }
                }
                else
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(this.ToString());
                        Logger.Instance.Error(e.SocketError.ToString());
                    }
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(this.ToString());
                    Logger.Instance.Error(ex);
                }

                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (callback != null)
            {
                callback.Closed(this, e.UserToken);
                e.Dispose();
                Close();
            }
        }

        public async Task SendAsync(Memory<byte> data, CancellationToken cancellationToken = default)
        {
            await Socket.SendAsync(data, SocketFlags.None, cancellationToken);

        }

        public void Close()
        {
            callback = null;
            Socket?.SafeClose();
        }

        public override string ToString()
        {
            return $"TransactionId:{TransactionId},TransportName:{TransportName},ProtocolType:{ProtocolType},Type:{Type},Direction:{Direction},IPEndPoint:{IPEndPoint},RemoteMachineName:{RemoteMachineName}";
        }
    }





}
