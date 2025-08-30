using linker.libs.extends;
using linker.libs;
using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Sockets;
using System;

namespace linker.tunnel.connection
{
    /// <summary>
    /// UDP隧道
    /// </summary>
    public sealed class TunnelConnectionUdp : ITunnelConnection
    {
        public TunnelConnectionUdp()
        {
        }

        public string RemoteMachineId { get; init; }
        public string RemoteMachineName { get; init; }
        public string TransactionId { get; init; }
        public string TransactionTag { get; init; }
        public string TransportName { get; init; }
        public string Label { get; init; }
        public TunnelMode Mode { get; init; }
        public TunnelProtocolType ProtocolType { get; init; }
        public TunnelType Type { get; init; }
        public string NodeId { get; init; }
        public TunnelDirection Direction { get; init; }
        public IPEndPoint IPEndPoint { get; init; }
        public bool SSL { get; init; }
        public byte BufferSize { get; init; } = 3;

        public bool Connected => UdpClient != null && LastTicks.Expired(15000) == false;
        public int Delay { get; private set; }
        public long SendBytes { get; private set; }
        public long ReceiveBytes { get; private set; }
        public LastTicksManager LastTicks { get; private set; } = new LastTicksManager();
        public byte[] SendBuffer { get; } = Helper.EmptyArray;

        public bool Receive { get; init; }

        public Socket uUdpClient;
        [JsonIgnore]
        public Socket UdpClient
        {
            get
            {
                return uUdpClient;
            }
            init
            {
                uUdpClient = value;
            }
        }
        [JsonIgnore]
        public ICrypto Crypto { get; init; }


        private ITunnelConnectionReceiveCallback callback;
        private CancellationTokenSource cancellationTokenSource;
        private object userToken;

        private readonly LastTicksManager pingTicks = new LastTicksManager();
        private readonly byte[] pingBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.ping");
        private readonly byte[] pongBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.pong");
        private readonly byte[] finBytes = Encoding.UTF8.GetBytes($"{Helper.GlobalString}.udp.fing");


        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="callback">数据回调</param>
        /// <param name="userToken">自定义数据</param>
        public void BeginReceive(ITunnelConnectionReceiveCallback callback, object userToken)
        {
            if (this.callback != null) return;

            this.callback = callback;
            this.userToken = userToken;

            cancellationTokenSource = new CancellationTokenSource();

            if (Receive)
            {
                _ = ProcessWrite();
            }
            _ = ProcessHeart();

        }
        private async Task ProcessWrite()
        {
            byte[] buffer = new byte[65 * 1024];
            IPEndPoint ep = new IPEndPoint(IPEndPoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    SocketReceiveFromResult result = await UdpClient.ReceiveFromAsync(buffer.AsMemory(), ep, cancellationTokenSource.Token).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error($"tunnel connection writer offline 0");
                        continue;
                    }
                    await CallbackPacket(buffer, 0, result.ReceivedBytes).ConfigureAwait(false);
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
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {cancellationTokenSource.IsCancellationRequested}");
                LoggerHelper.Instance.Error($"tunnel connection disponse 6");
                Dispose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"tunnel connection writer offline {ToString()}");
            }
        }
        public async Task<bool> ProcessWrite(byte[] buffer, int offset, int length)
        {
            if (callback == null)
            {
                return false;
            }
            await CallbackPacket(buffer, offset, length).ConfigureAwait(false);
            return true;
        }
        private async Task CallbackPacket(byte[] buffer, int offset, int length)
        {
            ReceiveBytes += length;
            LastTicks.Update();

            Memory<byte> memory = buffer.AsMemory(offset, length);

            if (memory.Length == pingBytes.Length && memory.Span.Slice(0, pingBytes.Length - 4).SequenceEqual(pingBytes.AsSpan(0, pingBytes.Length - 4)))
            {
                if (memory.Span.SequenceEqual(pingBytes))
                {
                    await SendPingPong(pongBytes).ConfigureAwait(false);
                }
                else if (memory.Span.SequenceEqual(pongBytes))
                {
                    Delay = (int)pingTicks.Diff();
                }
                else if (memory.Span.SequenceEqual(finBytes))
                {
                    LoggerHelper.Instance.Error($"tunnel connection disponse 5");
                    Dispose();
                }
            }
            else
            {
                try
                {
                    if (SSL)
                    {
                        memory = Crypto.Decode(buffer, offset, length);
                    }
                    await callback.Receive(this, memory, this.userToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                        LoggerHelper.Instance.Error(Encoding.UTF8.GetString(memory.Span));
                        LoggerHelper.Instance.Error(string.Join(",", memory.ToArray()));
                    }

                }
            }
        }

        private async Task ProcessHeart()
        {
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (Connected == false)
                    {
                        LoggerHelper.Instance.Error($"tunnel connection disponse 4");
                        Dispose();
                        break;
                    }

                    if (LastTicks.DiffGreater(3000))
                    {
                        pingTicks.Update();
                        await SendPingPong(pingBytes).ConfigureAwait(false);
                    }
                    await Task.Delay(3000).ConfigureAwait(false);
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
        private async Task SendPingPong(byte[] data)
        {
            int length = 0;

            byte[] heartData = ArrayPool<byte>.Shared.Rent(1024);
            Memory<byte> memory = heartData.AsMemory();

            //中继包头
            if (Type == TunnelType.Relay)
            {
                length += 2;
                heartData[0] = 2; //relay
                heartData[1] = 1; //forward
                memory = memory.Slice(2);
            }
            //真的数据
            data.AsMemory().CopyTo(memory);
            length += data.Length;

            SendBytes += length;
            try
            {
                await UdpClient.SendToAsync(heartData.AsMemory(0, length), IPEndPoint, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                LoggerHelper.Instance.Error($"tunnel connection disponse 3");
                Dispose();
            }
            finally
            {
            }

            ArrayPool<byte>.Shared.Return(heartData);
        }

        private byte[] encodeBuffer = new byte[65 * 1024];
        public async Task<bool> SendAsync(ReadOnlyMemory<byte> data)
        {
            try
            {
                data = data.Slice(4);

                if (SSL)
                {
                    data.CopyTo(encodeBuffer);
                    data = Crypto.Encode(encodeBuffer, 0, data.Length);
                }
                if (Type == TunnelType.Relay)
                {
                    data.CopyTo(encodeBuffer.AsMemory(2));
                    encodeBuffer[0] = 2; //relay
                    encodeBuffer[1] = 1; //forward 
                    data = encodeBuffer.AsMemory(0, data.Length + 2);
                }
                await UdpClient.SendToAsync(data, IPEndPoint, cancellationTokenSource.Token).ConfigureAwait(false);
                SendBytes += data.Length;
                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                LoggerHelper.Instance.Error($"tunnel connection disponse 2");
                Dispose();
            }
            finally
            {
            }
            return false;
        }
        public async Task<bool> SendAsync(byte[] buffer, int offset, int length)
        {
            try
            {
                Memory<byte> data = buffer.AsMemory(offset + 4, length - 4);

                if (SSL)
                {
                    data = Crypto.Encode(buffer, offset + 4, length - 4);
                }
                if (Type == TunnelType.Relay)
                {
                    data.CopyTo(encodeBuffer.AsMemory(2));
                    encodeBuffer[0] = 2; //relay
                    encodeBuffer[1] = 1; //forward 
                    data = encodeBuffer.AsMemory(0, data.Length + 2);
                }
                await UdpClient.SendToAsync(data, IPEndPoint, cancellationTokenSource.Token).ConfigureAwait(false);
                SendBytes += data.Length;
                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                LoggerHelper.Instance.Error($"tunnel connection disponse 1");
                Dispose();

            }
            finally
            {
            }
            return false;
        }

        public void Dispose()
        {
            if (uUdpClient == null) return;

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"tunnel connection {this.GetHashCode()} writer offline {ToString()}");

            SendPingPong(finBytes).ContinueWith((result) =>
            {
                LastTicks.Clear();
                if (Receive == true)
                    UdpClient?.SafeClose();
                uUdpClient = null;

                cancellationTokenSource?.Cancel();
                callback?.Closed(this, userToken);
                callback = null;
                userToken = null;

                Crypto?.Dispose();

                GC.Collect();
            });
        }
        public override string ToString()
        {
            return this.ToJsonFormat();
        }
        public bool Equals(ITunnelConnection connection)
        {
            return connection != null && GetHashCode() == connection.GetHashCode() && TransactionId == connection.TransactionId && IPEndPoint.Equals(connection.IPEndPoint);
        }
    }
}