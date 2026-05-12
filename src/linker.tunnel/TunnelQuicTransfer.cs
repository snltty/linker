using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.tunnel.connection;
using linker.tunnel.transport;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace linker.tunnel
{
#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
    public sealed class TunnelQuicTransfer
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<(QuicConnection connection, QuicStream stream)>> dic = new();
        private IPEndPoint quicListenEP = new IPEndPoint(IPAddress.Any, 0);
        public void Listen(X509Certificate certificate)
        {
            TestQuic();
            _ = QuicListen(certificate);
        }
        public async Task<ITunnelConnection> Transform(ITunnelConnection connection, TunnelTransportInfo info)
        {
            if (connection is TunnelConnectionUdp udp == false || udp.TransactionId == "tuntap" || string.IsNullOrWhiteSpace(info.TransactionTag) == false)
            {
                return connection;
            }
            udp.Send = false;
            string key0 = $"{info.Remote.MachineId}->{info.TransactionId}->{info.FlowId}";
            string key1 = $"{info.Local.MachineId}->{info.TransactionId}->{info.FlowId}";
            Socket quicUdp = null;
            try
            {
                if (udp.Mode == TunnelMode.Client)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    quicUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    quicUdp.Bind(new IPEndPoint(IPAddress.Any, 0));
                    quicUdp.WindowsUdpBug();

                    TunnelCallback callback = new TunnelCallback(quicUdp, null, udp);
                    _ = callback.Receive().ConfigureAwait(false);
                    udp.BeginReceive(callback, null);

                    using CancellationTokenSource cts = new CancellationTokenSource(3000);
                    QuicConnection quicConnection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                    {
                        RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, (quicUdp.LocalEndPoint as IPEndPoint).Port),
                        LocalEndPoint = new IPEndPoint(IPAddress.Any, 0),
                        DefaultCloseErrorCode = 0x0a,
                        DefaultStreamErrorCode = 0x0b,
                        IdleTimeout = TimeSpan.FromMilliseconds(15000),
                        ClientAuthenticationOptions = new SslClientAuthenticationOptions
                        {
                            ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                            {
                                return true;
                            }
                        }
                    }, cts.Token).ConfigureAwait(false);
                    QuicStream quicStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional).ConfigureAwait(false);

                    await quicStream.WriteAsync(Encoding.UTF8.GetBytes(key0)).ConfigureAwait(false);
                    TunnelConnectionQuic quic = new TunnelConnectionQuic
                    {
                        QuicUdp = quicUdp,
                        RemoteUdp = udp.UdpClient,
                        Stream = quicStream,
                        Connection = quicConnection,
                        IPEndPoint = udp.IPEndPoint.MapToIPv4(),
                        TransactionId = info.TransactionId,
                        TransactionTag = info.TransactionTag,
                        RemoteMachineId = info.Remote.MachineId,
                        RemoteMachineName = info.Remote.MachineName,
                        TransportName = info.TransportName,
                        Direction = info.Direction,
                        ProtocolType = TunnelProtocolType.Quic,
                        Type = connection.Type,
                        Mode = connection.Mode,
                        Label = connection.Label,
                        BufferSize = info.BufferSize,
                        OriginConnection = connection,
                    };
                    callback.SetQuic(quic);
                    return quic;
                }
                else
                {

                    quicUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    quicUdp.WindowsUdpBug();

                    TunnelCallback callback = new TunnelCallback(quicUdp, quicListenEP, udp);
                    udp.BeginReceive(callback, null);

                    TaskCompletionSource<(QuicConnection connection, QuicStream stream)> tcs = new();
                    dic.AddOrUpdate(key1, tcs, (k, v) => tcs);
                    (QuicConnection quicConnection, QuicStream quicStream) = await tcs.WithTimeout(3000).ConfigureAwait(false);
                    TunnelConnectionQuic quic = new TunnelConnectionQuic
                    {
                        QuicUdp = quicUdp,
                        RemoteUdp = udp.UdpClient,
                        Stream = quicStream,
                        Connection = quicConnection,
                        IPEndPoint = udp.IPEndPoint.MapToIPv4(),
                        TransactionId = info.TransactionId,
                        TransactionTag = info.TransactionTag,
                        RemoteMachineId = info.Remote.MachineId,
                        RemoteMachineName = info.Remote.MachineName,
                        TransportName = info.TransportName,
                        Direction = info.Direction,
                        ProtocolType = TunnelProtocolType.Quic,
                        Type = connection.Type,
                        Mode = connection.Mode,
                        Label = connection.Label,
                        BufferSize = info.BufferSize,
                        OriginConnection = connection,
                    };
                    callback.SetQuic(quic);
                    return quic;

                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
                quicUdp?.SafeClose();
            }
            finally
            {
                dic.TryRemove(key1, out _);
            }

            connection?.Dispose();
            return null;
        }

        private async Task QuicListen(X509Certificate certificate)
        {
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                try
                {
                    if (QuicListener.IsSupported == false)
                    {
                        LoggerHelper.Instance.Warning($"msquic not supported, need win11+,or linux, or try to restart linker");
                        return;
                    }
                    if (certificate == null)
                    {
                        LoggerHelper.Instance.Warning($"msquic need ssl");
                        return;
                    }

                    QuicListener listener = await QuicListener.ListenAsync(new QuicListenerOptions
                    {
                        ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                        ListenBacklog = int.MaxValue,
                        ListenEndPoint = new IPEndPoint(IPAddress.Any, 0),
                        ConnectionOptionsCallback = (connection, hello, token) =>
                        {
                            return ValueTask.FromResult(new QuicServerConnectionOptions
                            {
                                MaxInboundBidirectionalStreams = 65535,
                                MaxInboundUnidirectionalStreams = 65535,
                                DefaultCloseErrorCode = 0x0a,
                                DefaultStreamErrorCode = 0x0b,
                                IdleTimeout = TimeSpan.FromMilliseconds(15000),
                                ServerAuthenticationOptions = new SslServerAuthenticationOptions
                                {
                                    ServerCertificate = certificate,
                                    EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                                    ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 }
                                }
                            });
                        }
                    }).ConfigureAwait(false);

                    quicListenEP = new IPEndPoint(IPAddress.Loopback, listener.LocalEndPoint.Port);
                    while (true)
                    {
                        try
                        {
                            QuicConnection quicConnection = await listener.AcceptConnectionAsync().ConfigureAwait(false);
                            TimerHelper.Async(async () =>
                            {
                                while (true)
                                {
                                    QuicStream quicStream = await quicConnection.AcceptInboundStreamAsync().ConfigureAwait(false);
                                    try
                                    {
                                        using CancellationTokenSource cts = new CancellationTokenSource(3000);
                                        using IMemoryOwner<byte> bufferOwner = MemoryPool<byte>.Shared.Rent(8 * 1024);
                                        int length = await quicStream.ReadAsync(bufferOwner.Memory, cts.Token).ConfigureAwait(false);
                                        string key = Encoding.UTF8.GetString(bufferOwner.Memory.Slice(0, length).Span);
                                        if (dic.TryRemove(key, out TaskCompletionSource<(QuicConnection connection, QuicStream stream)> tcs))
                                        {
                                            tcs.TrySetResult((quicConnection, quicStream));
                                        }
                                        else
                                        {
                                            quicStream.Close();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        quicStream?.Close();
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            {
                                LoggerHelper.Instance.Error(ex);
                            }
                            break;
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
            }
        }

        private void TestQuic()
        {
            if (OperatingSystem.IsWindows() && QuicListener.IsSupported == false && File.Exists("msquic-openssl.dll"))
            {
                try
                {
                    LoggerHelper.Instance.Warning($"copy msquic-openssl.dll -> msquic.dll，please restart linker");
                    File.Move("msquic-openssl.dll", "msquic.dll", true);

                    if (Environment.UserInteractive == false)
                    {
                        Helper.AppExit(1);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public sealed class TunnelCallback : ITunnelConnectionReceiveCallback
    {
        private ITunnelConnection quic;
        private readonly Socket localUdp;
        private IPEndPoint localEp;
        private readonly Socket remoteUdp;
        private readonly IPEndPoint remoteEp;
        private bool first = true;

        private readonly CancellationTokenSource cts = new();

        public TunnelCallback(Socket localUdp, IPEndPoint localEp, TunnelConnectionUdp udp)
        {
            this.localUdp = localUdp;
            this.localEp = localEp;
            this.remoteUdp = udp.UdpClient;
            this.remoteEp = udp.IPEndPoint;

            first = localEp != null;
        }

        public void SetQuic(ITunnelConnection quic)
        {
            this.quic = quic;
        }

        public Task Closed(ITunnelConnection connection, object state)
        {
            cts?.Cancel();
            quic?.Dispose();
            return Task.CompletedTask;
        }

        public async Task<bool> Receive()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            using CancellationTokenSource cts = new(1000);
            try
            {
                SocketReceiveFromResult result = await localUdp.ReceiveFromAsync(buffer, tempEp, cts.Token).ConfigureAwait(false);
                localEp = result.RemoteEndPoint as IPEndPoint;
                await remoteUdp.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), remoteEp).ConfigureAwait(false);

                _ = CopyToAsync(localUdp, remoteUdp, remoteEp);

                return true;
            }
            catch (Exception)
            {
                localUdp?.SafeClose();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return false;
        }
        public async Task Receive(ITunnelConnection connection, ReadOnlyMemory<byte> data, object state)
        {
            await localUdp.SendToAsync(data, localEp).ConfigureAwait(false);

            if (first)
            {
                first = false;
                _ = CopyToAsync(localUdp, remoteUdp, remoteEp).ConfigureAwait(false);
            }

        }
        private async Task CopyToAsync(Socket local, Socket remote, IPEndPoint remoteEp)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            IPEndPoint tempEp = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            try
            {
                while (true)
                {
                    SocketReceiveFromResult result = await local.ReceiveFromAsync(buffer, tempEp, cts.Token).ConfigureAwait(false);
                    if (result.ReceivedBytes == 0)
                    {
                        continue;
                    }
                    await remote.SendToAsync(buffer.AsMemory(0, result.ReceivedBytes), remoteEp).ConfigureAwait(false);
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
                local?.SafeClose();
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
