using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace linker.discovery
{
    public sealed class DiscoveryRelaySession : IDisposable
    {
        private static readonly TimeSpan QueryTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RecentFallbackTtl = TimeSpan.FromSeconds(5);

        private static readonly IPEndPoint AnyEndpoint = new(IPAddress.Any, 0);
        private readonly DiscoveryProtocolInfo _protocol;
        private readonly IPAddress _tunIp;
        private readonly List<LanInterface> _lanInterfaces;
        private readonly HashSet<IPAddress> _lanIps;
        private readonly IDiscoveryProtocolMatcher _matcher;
        private readonly DiscoveryRelayQueryTracker _tracker;
        private readonly Socket _socket;
        private readonly Socket _tunSendSocket;
        private readonly int _tunInterfaceIndex;
        private readonly IPv4Network _tunNetwork;
        private readonly Action<DiscoveryRelayError> _raiseError;
        private readonly bool _allowRecentFallback;
        private readonly bool _receiveLanResponsesOnForwardSockets;
        private readonly DiscoveryProtocolRecentPacketCache _sentToLan = new(TimeSpan.FromSeconds(2));
        private readonly DiscoveryProtocolRecentPacketCache _sentToTun = new(TimeSpan.FromSeconds(2));
        private Task? _receiveTask;

        public DiscoveryRelaySession(
            DiscoveryProtocolInfo protocol,
            IPAddress tunIp,
            IReadOnlyList<IPAddress> lanIps,
            Action<DiscoveryRelayError> raiseError)
        {
            _protocol = protocol;
            _tunIp = tunIp;
            _raiseError = raiseError;
            _tracker = new DiscoveryRelayQueryTracker(QueryTtl, RecentFallbackTtl);
            _matcher = protocol.Matcher ?? DiscoveryProtocolMatcherSelector.Select(protocol);
            _allowRecentFallback = protocol.Matcher is not null || _matcher is DiscoveryProtocolMatcherPayloadHash;
            _receiveLanResponsesOnForwardSockets = ShouldReceiveLanResponsesOnForwardSockets(protocol, _matcher);
            _tunInterfaceIndex = GetInterfaceIndex(tunIp);
            if (_tunInterfaceIndex == 0)
            {
                throw new InvalidOperationException($"Unable to find an IPv4 interface for TUN address {tunIp}.");
            }

            _tunNetwork = GetNetworkRange(tunIp);
            _lanInterfaces = new List<LanInterface>(lanIps.Count);
            _lanIps = new HashSet<IPAddress>();

            Socket? socket = null;
            Socket? tunSendSocket = null;

            try
            {
                foreach (IPAddress lanIp in lanIps)
                {
                    int lanInterfaceIndex = GetInterfaceIndex(lanIp);
                    if (lanInterfaceIndex == 0)
                    {
                        throw new InvalidOperationException($"Unable to find an IPv4 interface for LAN address {lanIp}.");
                    }

                    IPAddress broadcastAddress = GetBroadcastAddress(lanIp);
                    byte[] multicastInterfaceBytes = lanIp.GetAddressBytes();
                    Socket? responseSocket = _receiveLanResponsesOnForwardSockets
                        ? CreateLanResponseSocket(protocol, lanIp, multicastInterfaceBytes)
                        : null;

                    _lanIps.Add(lanIp);
                    _lanInterfaces.Add(new LanInterface(
                        lanIp,
                        lanInterfaceIndex,
                        broadcastAddress,
                        GetNetworkRange(lanIp),
                        GetLanTarget(protocol, broadcastAddress),
                        multicastInterfaceBytes,
                        responseSocket));
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
            foreach (LanInterface lan in _lanInterfaces)
            {
                if (lan.ResponseSocket is not null)
                {
                    lan.ResponseReceiveTask = Task.Run(() => RunLanResponseReceiveAsync(lan, cancellationToken), CancellationToken.None);
                }
            }
        }

        public void AddTasks(List<Task> tasks)
        {
            if (_receiveTask is not null)
            {
                tasks.Add(_receiveTask);
            }

            foreach (LanInterface lan in _lanInterfaces)
            {
                if (lan.ResponseReceiveTask is not null)
                {
                    tasks.Add(lan.ResponseReceiveTask);
                }
            }
        }

        public void Dispose()
        {
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
                        await RelayTunToLanAsync(remote, packet, hash, keys, cancellationToken).ConfigureAwait(false);
                    }
                    else if (source == PacketSource.Lan)
                    {
                        await RelayLanToTunAsync(remote, packet, hash, keys, destinations, cancellationToken).ConfigureAwait(false);
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

        private async Task RunLanResponseReceiveAsync(LanInterface lan, CancellationToken cancellationToken)
        {
            if (lan.ResponseSocket is null)
            {
                return;
            }

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
                        result = await lan.ResponseSocket
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

                    await RelayLanToTunAsync(remote, packet, hash, keys, destinations, cancellationToken).ConfigureAwait(false);
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
            if (_matcher.GetQueryKeys(_protocol, packet.Span, keys) == 0)
            {
                DiscoveryProtocolKeyHelper.AddPayloadHashKey(keys, hash);
            }

            _tracker.Remember(keys, remote);
            _sentToLan.Add(hash);

            foreach (LanInterface lan in _lanInterfaces)
            {
                try
                {
                    Socket socket = lan.ResponseSocket ?? _socket;
                    if (_protocol.Type == DiscoveryProtocolType.Multicast && lan.ResponseSocket is null)
                    {
                        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, lan.MulticastInterfaceBytes);
                    }

                    await socket.SendToAsync(packet, SocketFlags.None, lan.Target, cancellationToken).ConfigureAwait(false);
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
            CancellationToken cancellationToken)
        {
            keys.Clear();
            _matcher.GetResponseKeys(_protocol, packet.Span, keys);

            destinations.Clear();
            if (keys.Count > 0)
            {
                _tracker.GetDestinations(keys, destinations);
            }

            if (destinations.Count == 0 && _allowRecentFallback)
            {
                _tracker.GetRecentDestinations(destinations);
            }

            if (destinations.Count == 0)
            {
                return;
            }

            _sentToTun.Add(hash);
            foreach (IPEndPoint destination in destinations)
            {
                try
                {
                    await _tunSendSocket.SendToAsync(packet, SocketFlags.None, destination, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsRecoverableSocketError(ex, cancellationToken))
                {
                    RaiseError("lan-to-tun", _tunIp, destination, ex);
                }
            }
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

        private static bool ShouldReceiveLanResponsesOnForwardSockets(
            DiscoveryProtocolInfo protocol,
            IDiscoveryProtocolMatcher matcher)
        {
            return protocol.Port is 137 or 1900 or 3702 or 5355 ||
                matcher is DiscoveryProtocolMatcherSsdp or
                    DiscoveryProtocolMatcherWs or
                    DiscoveryProtocolMatcherLlmnr or
                    DiscoveryProtocolMatcherNbns;
        }

        private static void JoinMulticast(Socket socket, IPAddress multicastAddress, IPAddress interfaceAddress)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, interfaceAddress));
        }

        private static IPAddress GetProtocolTargetAddress(DiscoveryProtocolInfo protocol)
        {
            if (protocol.Type == DiscoveryProtocolType.Broadcast && IsAny(protocol.Address))
            {
                return IPAddress.Broadcast;
            }

            return protocol.Address;
        }

        private static IPEndPoint GetLanTarget(DiscoveryProtocolInfo protocol, IPAddress broadcastAddress)
        {
            if (protocol.Type == DiscoveryProtocolType.Broadcast &&
                (IsAny(protocol.Address) || protocol.Address.Equals(IPAddress.Broadcast)))
            {
                return new IPEndPoint(broadcastAddress, protocol.Port);
            }

            return new IPEndPoint(GetProtocolTargetAddress(protocol), protocol.Port);
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

        private static int GetInterfaceIndex(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties properties;
                IPv4InterfaceProperties? ipv4Properties;
                try
                {
                    properties = adapter.GetIPProperties();
                    ipv4Properties = properties.GetIPv4Properties();
                }
                catch (NetworkInformationException)
                {
                    continue;
                }

                if (ipv4Properties is null)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.Equals(address))
                    {
                        return ipv4Properties.Index;
                    }
                }
            }

            return 0;
        }

        private static IPAddress GetBroadcastAddress(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties properties;
                try
                {
                    properties = adapter.GetIPProperties();
                }
                catch (NetworkInformationException)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (!unicast.Address.Equals(address) ||
                        unicast.IPv4Mask is null ||
                        unicast.IPv4Mask.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    Span<byte> ipBytes = stackalloc byte[4];
                    Span<byte> maskBytes = stackalloc byte[4];
                    if (!address.TryWriteBytes(ipBytes, out _) ||
                        !unicast.IPv4Mask.TryWriteBytes(maskBytes, out _))
                    {
                        break;
                    }

                    Span<byte> broadcast = stackalloc byte[4];
                    for (int i = 0; i < broadcast.Length; i++)
                    {
                        broadcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                    }

                    return new IPAddress(broadcast);
                }
            }

            return IPAddress.Broadcast;
        }

        private static IPv4Network GetNetworkRange(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties properties;
                try
                {
                    properties = adapter.GetIPProperties();
                }
                catch (NetworkInformationException)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.Equals(address) &&
                        unicast.IPv4Mask is not null &&
                        unicast.IPv4Mask.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IPv4Network.Create(address, unicast.IPv4Mask);
                    }
                }
            }

            return IPv4Network.Create(address, IPAddress.Broadcast);
        }

        private sealed class LanInterface : IDisposable
        {
            public LanInterface(
                IPAddress address,
                int interfaceIndex,
                IPAddress broadcastAddress,
                IPv4Network network,
                IPEndPoint target,
                byte[] multicastInterfaceBytes,
                Socket? responseSocket)
            {
                Address = address;
                InterfaceIndex = interfaceIndex;
                BroadcastAddress = broadcastAddress;
                Network = network;
                Target = target;
                MulticastInterfaceBytes = multicastInterfaceBytes;
                ResponseSocket = responseSocket;
            }

            public IPAddress Address { get; }

            public int InterfaceIndex { get; }

            public IPAddress BroadcastAddress { get; }

            public IPv4Network Network { get; }

            public IPEndPoint Target { get; }

            public byte[] MulticastInterfaceBytes { get; }

            public Socket? ResponseSocket { get; }

            public Task? ResponseReceiveTask { get; set; }

            public void Dispose()
            {
                ResponseSocket?.Dispose();
            }
        }

        private readonly record struct IPv4Network(uint Address, uint Mask)
        {
            public static IPv4Network Create(IPAddress address, IPAddress mask)
            {
                if (!TryToUInt32(address, out uint addressValue) ||
                    !TryToUInt32(mask, out uint maskValue))
                {
                    return default;
                }

                return new IPv4Network(addressValue & maskValue, maskValue);
            }

            public bool Contains(IPAddress address)
            {
                return Mask != 0 &&
                    TryToUInt32(address, out uint addressValue) &&
                    (addressValue & Mask) == Address;
            }

            private static bool TryToUInt32(IPAddress address, out uint value)
            {
                Span<byte> bytes = stackalloc byte[4];
                if (!address.TryWriteBytes(bytes, out int written) || written != 4)
                {
                    value = 0;
                    return false;
                }

                value = ((uint)bytes[0] << 24) |
                    ((uint)bytes[1] << 16) |
                    ((uint)bytes[2] << 8) |
                    bytes[3];
                return true;
            }
        }

        private static bool IsAny(IPAddress address)
        {
            return address.Equals(IPAddress.Any) || address.Equals(IPAddress.None);
        }

        private enum PacketSource
        {
            Unknown,
            Tun,
            Lan
        }
    }
}
