using common.libs.extends;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace cmonitor.client.tunnel
{
    public delegate Task TunnelReceivceCallback(ITunnelConnection connection,Memory<byte> data, object state);
    public delegate Task TunnelCloseCallback(ITunnelConnection connection,object state);

    public enum TunnelProtocolType : byte
    {
        Tcp = ProtocolType.Tcp,
        Udp = ProtocolType.Udp,
    }
    public enum TunnelType:byte
    {
        P2P = 0,
        Relay = 1,
    }
    public enum TunnelDirection : byte
    {
        Forward = 0,
        Reverse = 1
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

        public bool Connected { get; }

        public Task SendAsync(Memory<byte> data);
        public void BeginReceive(TunnelReceivceCallback receiveCallback, TunnelCloseCallback closeCallback, object userToken);

        public void Close();
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

        public bool Connected => Socket != null && Socket.Connected;

        [JsonIgnore]
        public Socket Socket { get; init; }


        private TunnelReceivceCallback receiveCallback;
        private TunnelCloseCallback closeCallback;

        public void BeginReceive(TunnelReceivceCallback receiveCallback, TunnelCloseCallback closeCallback, object userToken)
        {
            if (this.receiveCallback != null) return;

            this.receiveCallback = receiveCallback;
            this.closeCallback = closeCallback;
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

                    await receiveCallback(this,e.Buffer.AsMemory(offset, length), e.UserToken).ConfigureAwait(false);

                    if (Socket.Available > 0)
                    {
                        while (Socket.Available > 0)
                        {
                            length = Socket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                await receiveCallback(this,e.Buffer.AsMemory(offset, length), e.UserToken).ConfigureAwait(false);
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
                    CloseClientSocket(e);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if(closeCallback != null)
            {
                closeCallback(this, e.UserToken);
                e.Dispose();
                Close();
            }
        }

        public async Task SendAsync(Memory<byte> data)
        {
            await Socket.SendAsync(data, SocketFlags.None);
        }

        public void Close()
        {
            receiveCallback = null;
            closeCallback = null;
            Socket?.SafeClose();
        }
    }


}
