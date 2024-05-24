using common.libs;
using common.libs.extends;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace cmonitor.server
{
    public sealed class TcpServer
    {
        private Socket socket;
        private UdpClient socketUdp;
        private CancellationTokenSource cancellationTokenSource;
        private Memory<byte> relayFLagCData = Encoding.UTF8.GetBytes("snltty.relay");
        private readonly X509Certificate serverCertificate;

        public Func<IConnection, Task> OnPacket { get; set; } = async (connection) => { await Task.CompletedTask; };

        public TcpServer()
        {
            string path = Path.GetFullPath("./snltty.pfx");
            if (File.Exists(path))
            {
                serverCertificate = new X509Certificate(path, "snltty");
            }
            else
            {
                Logger.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }

        }
        public void Start(int port)
        {
            if (socket == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
                socket = BindAccept(port);
            }
        }

        private Socket BindAccept(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(NetworkHelper.IPv6Support ? IPAddress.IPv6Any : IPAddress.Any, port);
            Socket socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.ReuseBind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = socket,
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);

            socketUdp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            //socketUdp.JoinMulticastGroup(config.BroadcastIP);
            socketUdp.Client.EnableBroadcast = true;
            socketUdp.Client.WindowsUdpBug();
            IAsyncResult result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);


            return socket;

        }

        byte[] sendData = ArrayPool<byte>.Shared.Rent(20);
        private async void ReceiveCallbackUdp(IAsyncResult result)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                byte[] bytes = socketUdp.EndReceive(result, ref endPoint);


                try
                {
                    sendData[0] = (byte)endPoint.AddressFamily;
                    endPoint.Address.TryWriteBytes(sendData.AsSpan(1), out int length);
                    ((ushort)endPoint.Port).ToBytes(sendData.AsMemory(1 + length));

                    for (int i = 0; i < 1 + length + 2; i++)
                    {
                        sendData[i] = (byte)(sendData[i] ^ byte.MaxValue);
                    }

                    await socketUdp.SendAsync(sendData.AsMemory(0, 1 + length + 2), endPoint);
                }
                catch (Exception)
                {
                }

                result = socketUdp.BeginReceive(ReceiveCallbackUdp, null);
            }
            catch (Exception)
            {
            }
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            Socket token = (Socket)acceptEventArg.UserToken;
            try
            {
                if (token.AcceptAsync(acceptEventArg) == false)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
                token?.SafeClose();
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
            if (e.AcceptSocket != null)
            {
                _ = BindReceiveServer(e.AcceptSocket);
                StartAccept(e);
            }
        }

        private async Task<IConnection> BindReceiveServer(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                SslStream sslStream = new SslStream(new NetworkStream(socket), true);
                await sslStream.AuthenticateAsServerAsync(serverCertificate, false, SslProtocols.Tls13, false);
                IConnection connection = CreateConnection(sslStream, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);
                _ = ProcessReceive(connection, sslStream);

                return connection;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            return null;
        }


        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public async Task<IConnection> BindReceive(Socket socket)
        {
            try
            {
                if (socket == null || socket.RemoteEndPoint == null)
                {
                    return null;
                }
                socket.KeepAlive();
                SslStream sslStream = new SslStream(new NetworkStream(socket), true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                await sslStream.AuthenticateAsClientAsync("snltty.com");
                IConnection connection = CreateConnection(sslStream, socket.LocalEndPoint as IPEndPoint, socket.RemoteEndPoint as IPEndPoint);
                _ = ProcessReceive(connection, sslStream);

                return connection;
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Error(ex);
            }
            return null;
        }
        private async Task ProcessReceive(IConnection connection, SslStream sslStream)
        {
            PipeReader reader = PipeReader.Create(sslStream);

            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    ReadResult readResult = await reader.ReadAsync().ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = readResult.Buffer;

                    SequencePosition end = await ReadPacket(connection, buffer).ConfigureAwait(false);
                    reader.AdvanceTo(buffer.Start, end);
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
                reader.Complete();
            }
        }
        private unsafe int ReaderHead(ReadOnlySequence<byte> buffer)
        {
            Span<byte> span = stackalloc byte[4];
            buffer.Slice(0, 4).CopyTo(span);
            return span.ToInt32();
        }

        private async Task<SequencePosition> ReadPacket(IConnection connection, ReadOnlySequence<byte> buffer)
        {
            //已中继
            if (connection.TcpTargetSocket != null)
            {
                SequencePosition position = buffer.Start;
                if (buffer.TryGet(ref position, out ReadOnlyMemory<byte> data))
                {
                    await connection.TcpTargetSocket.WriteAsync(data).ConfigureAwait(false);
                    await connection.TcpTargetSocket.FlushAsync();
                }
                return buffer.End;
            }
            //中继标识
            else if (buffer.Length == relayFLagCData.Length)
            {
                SequencePosition position = buffer.Start;
                if (buffer.TryGet(ref position, out ReadOnlyMemory<byte> data) && data.Span.SequenceEqual(relayFLagCData.Span))
                {
                    return buffer.End;
                }
            }
            //正常处理
            while (buffer.Length > 4)
            {
                int length = ReaderHead(buffer);
                if (buffer.Length < length + 4)
                {
                    break;
                }
                SequencePosition position = buffer.GetPosition(4);
                if (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    connection.ReceiveData = memory;
                    await OnPacket(connection).ConfigureAwait(false);
                }

                SequencePosition endPosition = buffer.GetPosition(4 + length);
                buffer = buffer.Slice(endPosition);
            }
            return buffer.Start;
        }

        public IConnection CreateConnection(SslStream stream, IPEndPoint local, IPEndPoint remote)
        {
            return new TcpConnection(stream, local, remote)
            {
                ReceiveRequestWrap = new MessageRequestWrap(),
                ReceiveResponseWrap = new MessageResponseWrap()
            };
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
            socket = null;
        }
        public void Disponse()
        {
            Stop();
            OnPacket = null;
        }
    }


    public sealed class AsyncUserToken
    {
        public IConnection Connection { get; set; }
        public Socket Socket { get; set; }
        public ReceiveDataBuffer DataBuffer { get; set; } = new ReceiveDataBuffer();
        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            Connection?.Disponse();
            Socket = null;

            PoolBuffer = Helper.EmptyArray;

            DataBuffer.Clear(true);

            GC.Collect();
            // GC.SuppressFinalize(this);
        }
    }
}
