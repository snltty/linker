using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace linker.discovery
{
    public sealed class DiscoveryRelaySession : IDisposable
    {
        private static readonly TimeSpan QueryTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RecentFallbackTtl = TimeSpan.FromSeconds(5);
        private const int MaxRequesterResponseSocketsPerLan = 512;

        private static readonly IPEndPoint AnyEndpoint = new(IPAddress.Any, 0);
        private readonly DiscoveryProtocolInfo _protocol;
        private readonly IPAddress _tunIp;
        private readonly List<LanInterface> _lanInterfaces;
        private readonly HashSet<IPAddress> _lanIps;
        private readonly IDiscoveryProtocolHandler _handler;
        private readonly DiscoveryProtocolRewriteContext _rewriteContext;
        private readonly DiscoveryRelayQueryTracker _tracker;
        private readonly Socket _socket;
        private readonly Socket _tunSendSocket;
        private readonly int _tunInterfaceIndex;
        private readonly DiscoveryProtocolHelper.IPv4Network _tunNetwork;
        private readonly Action<DiscoveryRelayError> _raiseError;
        private readonly Action<DiscoveryRelayAddressRewrite> _raiseAddressRewrite;
        private readonly Action<DiscoveryRelayPayloadRewrite> _raisePayloadRewrite;
        private readonly Action<DiscoveryRelayPacketTrace> _raisePacketTrace;
        private readonly bool _allowRecentFallback;
        private readonly bool _receiveLanResponsesOnForwardSockets;
        private readonly DiscoveryProtocolRecentPacketCache _sentToLan = new(TimeSpan.FromSeconds(2));
        private readonly DiscoveryProtocolRecentPacketCache _sentToTun = new(TimeSpan.FromSeconds(2));
        private Task? _receiveTask;
        private volatile bool _disposed;

        public DiscoveryRelaySession(
            DiscoveryProtocolInfo protocol,
            IPAddress tunIp,
            IReadOnlyList<IPAddress> lanIps,
            Action<DiscoveryRelayError> raiseError)
            : this(protocol, tunIp, lanIps, Array.Empty<DiscoveryAddressMapEntry>(), raiseError, static _ => { }, static _ => { }, static _ => { })
        {
        }

        internal DiscoveryRelaySession(
            DiscoveryProtocolInfo protocol,
            IPAddress tunIp,
            IReadOnlyList<IPAddress> lanIps,
            IReadOnlyList<DiscoveryAddressMapEntry> addressMaps,
            Action<DiscoveryRelayError> raiseError,
            Action<DiscoveryRelayAddressRewrite> raiseAddressRewrite,
            Action<DiscoveryRelayPayloadRewrite> raisePayloadRewrite,
            Action<DiscoveryRelayPacketTrace> raisePacketTrace)
        {
            _protocol = protocol;
            _tunIp = tunIp;
            _raiseError = raiseError;
            _raiseAddressRewrite = raiseAddressRewrite;
            _raisePayloadRewrite = raisePayloadRewrite;
            _raisePacketTrace = raisePacketTrace;
            _tracker = new DiscoveryRelayQueryTracker(QueryTtl, RecentFallbackTtl);
            _handler = protocol.Handler ?? DiscoveryProtocolHandlerSelector.Select(protocol);
            _rewriteContext = new DiscoveryProtocolRewriteContext(protocol, addressMaps, RaiseAddressRewrite, RaisePayloadRewrite);
            _allowRecentFallback = _handler.AllowRecentResponseFallback;
            _receiveLanResponsesOnForwardSockets = _handler.ReceiveLanResponsesOnForwardSockets;
            _tunInterfaceIndex = DiscoveryProtocolHelper.GetInterfaceIndex(tunIp);
            if (_tunInterfaceIndex == 0)
            {
                throw new InvalidOperationException($"Unable to find an IPv4 interface for TUN address {tunIp}.");
            }

            _tunNetwork = DiscoveryProtocolHelper.GetNetworkRange(tunIp);
            _lanInterfaces = new List<LanInterface>(lanIps.Count);
            _lanIps = new HashSet<IPAddress>();

            Socket? socket = null;
            Socket? tunSendSocket = null;

            try
            {
                foreach (IPAddress lanIp in lanIps)
                {
                    int lanInterfaceIndex = DiscoveryProtocolHelper.GetInterfaceIndex(lanIp);
                    if (lanInterfaceIndex == 0)
                    {
                        throw new InvalidOperationException($"Unable to find an IPv4 interface for LAN address {lanIp}.");
                    }

                    IPAddress broadcastAddress = DiscoveryProtocolHelper.GetBroadcastAddress(lanIp);
                    byte[] multicastInterfaceBytes = lanIp.GetAddressBytes();

                    _lanIps.Add(lanIp);
                    _lanInterfaces.Add(new LanInterface(
                        lanIp,
                        lanInterfaceIndex,
                        broadcastAddress,
                        DiscoveryProtocolHelper.GetNetworkRange(lanIp),
                        DiscoveryProtocolHelper.GetLanTarget(protocol, broadcastAddress),
                        multicastInterfaceBytes));
                }

                socket = CreateSocket(protocol, tunIp, _lanInterfaces);
                tunSendSocket = CreateTunSendSocket(protocol, tunIp);
                _socket = socket;
                _tunSendSocket = tunSendSocket;
            }
            catch
            {
                socket?.Dispose();
                tunSendSocket?.Dispose();
                foreach (LanInterface lan in _lanInterfaces)
                {
                    lan.Dispose();
                }

                throw;
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            _receiveTask = Task.Run(() => RunReceiveAsync(cancellationToken), CancellationToken.None);
        }

        public void AddTasks(List<Task> tasks)
        {
            if (_receiveTask is not null)
            {
                tasks.Add(_receiveTask);
            }

            foreach (LanInterface lan in _lanInterfaces)
            {
                lock (lan.Gate)
                {
                    foreach (RequesterResponseSocket responseSocket in lan.ResponseSockets.Values)
                    {
                        if (responseSocket.ReceiveTask is not null)
                        {
                            tasks.Add(responseSocket.ReceiveTask);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _socket.Dispose();
            _tunSendSocket.Dispose();
            foreach (LanInterface lan in _lanInterfaces)
            {
                lan.Dispose();
            }
        }

        private async Task RunReceiveAsync(CancellationToken cancellationToken)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(65535);
            var keys = new List<string>(4);
            var destinations = new List<IPEndPoint>(4);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    SocketReceiveMessageFromResult result;
                    try
                    {
                        result = await _socket
                            .ReceiveMessageFromAsync(rented.AsMemory(0, 65535), SocketFlags.None, AnyEndpoint, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (IsExpectedStop(ex, cancellationToken))
                    {
                        break;
                    }

                    if (result.RemoteEndPoint is not IPEndPoint remote ||
                        remote.AddressFamily != AddressFamily.InterNetwork ||
                        result.ReceivedBytes == 0)
                    {
                        continue;
                    }

                    ReadOnlyMemory<byte> packet = rented.AsMemory(0, result.ReceivedBytes);
                    ulong hash = DiscoveryProtocolPacketHash.Compute(packet.Span);
                    if (IsOwnTunEcho(remote, hash) || IsOwnLanEcho(remote, hash))
                    {
                        continue;
                    }

                    PacketSource source = GetPacketSource(result.PacketInformation.Interface, remote.Address);
                    if (source == PacketSource.Tun)
                    {
                        RaisePacketTrace("tun-receive", _tunIp, remote, null, packet.Length, "received discovery query from TUN");
                        try
                        {
                            await RelayTunToLanAsync(remote, packet, hash, keys, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                        {
                            RaiseError("tun-to-lan-process", _tunIp, remote, ex);
                        }
                    }
                    else if (source == PacketSource.Lan)
                    {
                        RaisePacketTrace("lan-receive", IPAddress.Any, remote, null, packet.Length, "received discovery response on shared protocol socket");
                        try
                        {
                            await RelayLanToTunAsync(remote, packet, hash, keys, destinations, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                        {
                            RaiseError("lan-to-tun-process", IPAddress.Any, remote, ex);
                        }
                    }
                    else
                    {
                        RaiseError("unknown-interface", IPAddress.Any, remote, new InvalidOperationException("Unable to classify received packet interface."));
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                RaiseError("receive", IPAddress.Any, null, ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        private async Task RunLanResponseReceiveAsync(
            LanInterface lan,
            RequesterResponseSocket responseSocket,
            CancellationToken cancellationToken)
        {
            byte[] rented = ArrayPool<byte>.Shared.Rent(65535);
            var keys = new List<string>(4);
            var destinations = new List<IPEndPoint>(4);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    SocketReceiveFromResult result;
                    try
                    {
                        result = await responseSocket.Socket
                            .ReceiveFromAsync(rented.AsMemory(0, 65535), SocketFlags.None, AnyEndpoint, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (IsExpectedStop(ex, cancellationToken))
                    {
                        break;
                    }

                    if (result.RemoteEndPoint is not IPEndPoint remote ||
                        remote.AddressFamily != AddressFamily.InterNetwork ||
                        result.ReceivedBytes == 0)
                    {
                        continue;
                    }

                    ReadOnlyMemory<byte> packet = rented.AsMemory(0, result.ReceivedBytes);
                    ulong hash = DiscoveryProtocolPacketHash.Compute(packet.Span);
                    if (IsOwnLanEcho(remote, hash))
                    {
                        continue;
                    }

                    RaisePacketTrace("lan-response-receive", lan.Address, remote, responseSocket.Requester, packet.Length, "received LAN response on requester-bound forward response socket");
                    try
                    {
                        await RelayLanToTunAsync(remote, packet, hash, keys, destinations, cancellationToken, responseSocket.Requester).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        RaiseError("lan-response-process", lan.Address, remote, ex);
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                RaiseError("lan-response-receive", lan.Address, null, ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        private async Task RelayTunToLanAsync(
            IPEndPoint remote,
            ReadOnlyMemory<byte> packet,
            ulong hash,
            List<string> keys,
            CancellationToken cancellationToken)
        {
            keys.Clear();
            if (_handler.GetQueryKeys(_protocol, packet.Span, keys) == 0)
            {
                DiscoveryProtocolKeyHelper.AddPayloadHashKey(keys, hash);
            }

            _tracker.Remember(keys, remote);
            _sentToLan.Add(hash);

            foreach (LanInterface lan in _lanInterfaces)
            {
                if (cancellationToken.IsCancellationRequested || _disposed)
                {
                    return;
                }

                try
                {
                    Socket socket;
                    if (_receiveLanResponsesOnForwardSockets)
                    {
                        RequesterResponseSocket? responseSocket = GetOrCreateRequesterResponseSocket(lan, remote, cancellationToken);
                        if (responseSocket is null)
                        {
                            return;
                        }

                        socket = responseSocket.Socket;
                    }
                    else
                    {
                        socket = _socket;
                    }

                    if (_protocol.Type == DiscoveryProtocolType.Multicast && !_receiveLanResponsesOnForwardSockets)
                    {
                        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, lan.MulticastInterfaceBytes);
                    }

                    await socket.SendToAsync(packet, SocketFlags.None, lan.Target, cancellationToken).ConfigureAwait(false);
                    RaisePacketTrace("tun-to-lan-send", lan.Address, remote, lan.Target, packet.Length, "forwarded TUN query to LAN");
                }
                catch (Exception ex) when (IsRecoverableSocketError(ex, cancellationToken))
                {
                    RaiseError("tun-to-lan", lan.Address, lan.Target, ex);
                }
            }
        }

        private async Task RelayLanToTunAsync(
            IPEndPoint remote,
            ReadOnlyMemory<byte> packet,
            ulong hash,
            List<string> keys,
            List<IPEndPoint> destinations,
            CancellationToken cancellationToken,
            IPEndPoint? boundDestination = null)
        {
            if (!TryResolveTunDestinationsForLanResponse(remote, packet, keys, destinations, boundDestination))
            {
                return;
            }

            // Rewrite only after this LAN packet has been accepted for relay to tracked TUN requester(s).
            ReadOnlyMemory<byte> sendPacket = _handler.RewritePayload(_rewriteContext, packet, out bool rewritten);

            ulong sendHash = rewritten ? DiscoveryProtocolPacketHash.Compute(sendPacket.Span) : hash;

            _sentToTun.Add(sendHash);
            foreach (IPEndPoint destination in destinations)
            {
                try
                {
                    await _tunSendSocket.SendToAsync(sendPacket, SocketFlags.None, destination, cancellationToken).ConfigureAwait(false);
                    RaisePacketTrace("lan-to-tun-send", _tunIp, remote, destination, sendPacket.Length, rewritten ? "forwarded rewritten LAN response to TUN" : "forwarded LAN response to TUN");
                }
                catch (Exception ex) when (IsRecoverableSocketError(ex, cancellationToken))
                {
                    RaiseError("lan-to-tun", _tunIp, destination, ex);
                }
            }
        }

        private bool TryResolveTunDestinationsForLanResponse(
            IPEndPoint remote,
            ReadOnlyMemory<byte> packet,
            List<string> keys,
            List<IPEndPoint> destinations,
            IPEndPoint? boundDestination)
        {
            destinations.Clear();
            keys.Clear();

            if (boundDestination is not null)
            {
                destinations.Add(boundDestination);
                return true;
            }

            _handler.GetResponseKeys(_protocol, packet.Span, keys);

            if (keys.Count > 0)
            {
                _tracker.GetDestinations(keys, destinations);
            }

            if (destinations.Count == 0 && _allowRecentFallback)
            {
                _tracker.GetRecentDestinations(destinations);
            }

            if (destinations.Count > 0)
            {
                return true;
            }

            RaisePacketTrace("lan-to-tun-drop", IPAddress.Any, remote, null, packet.Length, keys.Count == 0 ? "no response keys matched" : "no tracked TUN requester matched response keys");
            return false;
        }

        private static Socket CreateSocket(
            DiscoveryProtocolInfo protocol,
            IPAddress tunIp,
            IReadOnlyList<LanInterface> lanInterfaces)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1024 * 1024);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1024 * 1024);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

            if (protocol.Type == DiscoveryProtocolType.Broadcast)
            {
                socket.EnableBroadcast = true;
            }

            socket.Bind(new IPEndPoint(IPAddress.Any, protocol.Port));

            if (protocol.Type == DiscoveryProtocolType.Multicast)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, protocol.Ttl);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                JoinMulticast(socket, protocol.Address, tunIp);
                foreach (LanInterface lan in lanInterfaces)
                {
                    JoinMulticast(socket, protocol.Address, lan.Address);
                }
            }

            return socket;
        }

        private static Socket CreateTunSendSocket(DiscoveryProtocolInfo protocol, IPAddress tunIp)
        {
            try
            {
                return CreateBoundTunSendSocket(tunIp, protocol.Port);
            }
            catch (SocketException)
            {
                return CreateBoundTunSendSocket(tunIp, 0);
            }
        }

        private static Socket CreateBoundTunSendSocket(IPAddress tunIp, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };

            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1024 * 1024);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1024 * 1024);
                socket.Bind(new IPEndPoint(tunIp, port));
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static Socket CreateLanResponseSocket(
            DiscoveryProtocolInfo protocol,
            IPAddress lanIp,
            byte[] multicastInterfaceBytes)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1024 * 1024);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1024 * 1024);

            if (protocol.Type == DiscoveryProtocolType.Broadcast)
            {
                socket.EnableBroadcast = true;
            }

            socket.Bind(new IPEndPoint(lanIp, 0));

            if (protocol.Type == DiscoveryProtocolType.Multicast)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, protocol.Ttl);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, multicastInterfaceBytes);
            }

            return socket;
        }

        private RequesterResponseSocket? GetOrCreateRequesterResponseSocket(
            LanInterface lan,
            IPEndPoint requester,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || _disposed)
            {
                return null;
            }

            EndpointKey key = EndpointKey.From(requester);

            lock (lan.Gate)
            {
                if (cancellationToken.IsCancellationRequested || _disposed)
                {
                    return null;
                }

                PruneRequesterResponseSockets(lan, DateTime.UtcNow);

                if (lan.ResponseSockets.TryGetValue(key, out RequesterResponseSocket? existing))
                {
                    existing.Touch();
                    return existing;
                }

                Socket socket = CreateLanResponseSocket(_protocol, lan.Address, lan.MulticastInterfaceBytes);
                var responseSocket = new RequesterResponseSocket(socket, new IPEndPoint(requester.Address, requester.Port));
                responseSocket.ReceiveTask = Task.Run(
                    () => RunLanResponseReceiveAsync(lan, responseSocket, cancellationToken),
                    CancellationToken.None);
                EnsureRequesterResponseSocketCapacity(lan);
                lan.ResponseSockets.Add(key, responseSocket);
                return responseSocket;
            }
        }

        private static void PruneRequesterResponseSockets(LanInterface lan, DateTime nowUtc)
        {
            List<EndpointKey>? stale = null;
            foreach ((EndpointKey key, RequesterResponseSocket responseSocket) in lan.ResponseSockets)
            {
                if (nowUtc - responseSocket.LastUsedUtc > QueryTtl)
                {
                    stale ??= [];
                    stale.Add(key);
                }
            }

            if (stale is null)
            {
                return;
            }

            foreach (EndpointKey key in stale)
            {
                if (lan.ResponseSockets.Remove(key, out RequesterResponseSocket? responseSocket))
                {
                    responseSocket.Dispose();
                }
            }
        }

        private static void EnsureRequesterResponseSocketCapacity(LanInterface lan)
        {
            while (lan.ResponseSockets.Count >= MaxRequesterResponseSocketsPerLan)
            {
                EndpointKey? oldestKey = null;
                DateTime oldestUsedUtc = DateTime.MaxValue;

                foreach ((EndpointKey key, RequesterResponseSocket responseSocket) in lan.ResponseSockets)
                {
                    if (responseSocket.LastUsedUtc < oldestUsedUtc)
                    {
                        oldestKey = key;
                        oldestUsedUtc = responseSocket.LastUsedUtc;
                    }
                }

                if (oldestKey is not { } keyToRemove ||
                    !lan.ResponseSockets.Remove(keyToRemove, out RequesterResponseSocket? removed))
                {
                    return;
                }

                removed.Dispose();
            }
        }

        private static void JoinMulticast(Socket socket, IPAddress multicastAddress, IPAddress interfaceAddress)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, interfaceAddress));
        }

        private PacketSource GetPacketSource(int interfaceIndex, IPAddress remoteAddress)
        {
            if (interfaceIndex != 0)
            {
                if (interfaceIndex == _tunInterfaceIndex)
                {
                    return PacketSource.Tun;
                }

                foreach (LanInterface lan in _lanInterfaces)
                {
                    if (interfaceIndex == lan.InterfaceIndex)
                    {
                        return PacketSource.Lan;
                    }
                }
            }

            bool isTun = _tunNetwork.Contains(remoteAddress);
            bool isLan = false;
            foreach (LanInterface lan in _lanInterfaces)
            {
                if (lan.Network.Contains(remoteAddress))
                {
                    isLan = true;
                    break;
                }
            }

            if (isTun && !isLan)
            {
                return PacketSource.Tun;
            }

            if (isLan && !isTun)
            {
                return PacketSource.Lan;
            }

            return PacketSource.Unknown;
        }

        private bool IsOwnTunEcho(IPEndPoint remote, ulong hash)
        {
            return remote.Address.Equals(_tunIp) && _sentToTun.Contains(hash);
        }

        private bool IsOwnLanEcho(IPEndPoint remote, ulong hash)
        {
            return _lanIps.Contains(remote.Address) && _sentToLan.Contains(hash);
        }

        private void RaiseError(string direction, IPAddress localAddress, EndPoint? remoteEndPoint, Exception exception)
        {
            _raiseError(new DiscoveryRelayError(_protocol, direction, localAddress, remoteEndPoint, exception));
        }

        private void RaiseAddressRewrite(IPAddress originalAddress, IPAddress mappedAddress)
        {
            _raiseAddressRewrite(new DiscoveryRelayAddressRewrite(
                _protocol,
                "lan-to-tun",
                originalAddress,
                mappedAddress));
        }

        private void RaisePayloadRewrite(string fieldName, string originalValue, string rewrittenValue)
        {
            _raisePayloadRewrite(new DiscoveryRelayPayloadRewrite(
                _protocol,
                "lan-to-tun",
                fieldName,
                originalValue,
                rewrittenValue));
        }

        private void RaisePacketTrace(string direction, IPAddress localAddress, EndPoint? remoteEndPoint, EndPoint? targetEndPoint, int bytes, string remark)
        {
            _raisePacketTrace(new DiscoveryRelayPacketTrace(
                _protocol,
                direction,
                localAddress,
                remoteEndPoint,
                targetEndPoint,
                bytes,
                remark));
        }

        private static bool IsExpectedStop(Exception ex, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ||
                   ex is OperationCanceledException ||
                   ex is ObjectDisposedException ||
                   ex is SocketException { SocketErrorCode: SocketError.OperationAborted };
        }

        private static bool IsRecoverableSocketError(Exception ex, CancellationToken cancellationToken)
        {
            return !cancellationToken.IsCancellationRequested &&
                   (ex is SocketException or ObjectDisposedException or OperationCanceledException);
        }

        private sealed class LanInterface : IDisposable
        {
            public LanInterface(
                IPAddress address,
                int interfaceIndex,
                IPAddress broadcastAddress,
                DiscoveryProtocolHelper.IPv4Network network,
                IPEndPoint target,
                byte[] multicastInterfaceBytes)
            {
                Address = address;
                InterfaceIndex = interfaceIndex;
                BroadcastAddress = broadcastAddress;
                Network = network;
                Target = target;
                MulticastInterfaceBytes = multicastInterfaceBytes;
            }

            public object Gate { get; } = new();

            public IPAddress Address { get; }

            public int InterfaceIndex { get; }

            public IPAddress BroadcastAddress { get; }

            public DiscoveryProtocolHelper.IPv4Network Network { get; }

            public IPEndPoint Target { get; }

            public byte[] MulticastInterfaceBytes { get; }

            public Dictionary<EndpointKey, RequesterResponseSocket> ResponseSockets { get; } = [];

            public void Dispose()
            {
                lock (Gate)
                {
                    foreach (RequesterResponseSocket responseSocket in ResponseSockets.Values)
                    {
                        responseSocket.Dispose();
                    }

                    ResponseSockets.Clear();
                }
            }
        }

        private sealed class RequesterResponseSocket : IDisposable
        {
            public RequesterResponseSocket(Socket socket, IPEndPoint requester)
            {
                Socket = socket;
                Requester = requester;
                LastUsedUtc = DateTime.UtcNow;
            }

            public Socket Socket { get; }

            public IPEndPoint Requester { get; }

            public DateTime LastUsedUtc { get; private set; }

            public Task? ReceiveTask { get; set; }

            public void Touch()
            {
                LastUsedUtc = DateTime.UtcNow;
            }

            public void Dispose()
            {
                Socket.Dispose();
            }
        }

        private readonly record struct EndpointKey(IPAddress Address, int Port)
        {
            public static EndpointKey From(IPEndPoint endpoint)
            {
                return new EndpointKey(endpoint.Address, endpoint.Port);
            }
        }

        private enum PacketSource
        {
            Unknown,
            Tun,
            Lan
        }
    }
}
