using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace linker.stun;

public sealed class StunClient
{
    public const int DefaultPort = 3478;

    public async ValueTask<StunBindingResult> QueryBindingAsync(
        string host,
        int port = DefaultPort,
        StunClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        options ??= new StunClientOptions();
        options.Validate();

        var resolveError = await TryResolveAsync(host, port, options, cancellationToken).ConfigureAwait(false);
        var serverEndPoint = resolveError.ServerEndPoint;
        if (resolveError.Error is not null)
        {
            return ResolveFailedBindingResult(host, resolveError.Error);
        }

        if (serverEndPoint is null)
        {
            return new StunBindingResult(
                StunBindingStatus.ResolveFailed,
                host,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                "No server address matched the requested address family.");
        }

        try
        {
            await using var transport = StunUdpTransport.Create(serverEndPoint, options);
            return await SendBindingTransactionAsync(
                transport,
                host,
                serverEndPoint,
                options,
                StunChangeRequest.None,
                cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            return SocketErrorResult(host, serverEndPoint, null, ex);
        }
    }

    public async ValueTask<StunNatBehaviorResult> DiscoverNatBehaviorAsync(
        string host,
        int port = DefaultPort,
        StunClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        options ??= new StunClientOptions();
        options.Validate();

        var resolveError = await TryResolveAsync(host, port, options, cancellationToken).ConfigureAwait(false);
        var serverEndPoint = resolveError.ServerEndPoint;
        if (resolveError.Error is not null)
        {
            var binding = ResolveFailedBindingResult(host, resolveError.Error);
            return new StunNatBehaviorResult(
                StunNatBehaviorStatus.ResolveFailed,
                binding,
                StunNatMappingBehavior.Unknown,
                StunNatFilteringBehavior.Unknown,
                null,
                null,
                null,
                null,
                binding.Message);
        }

        if (serverEndPoint is null)
        {
            var binding = new StunBindingResult(
                StunBindingStatus.ResolveFailed,
                host,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                "No server address matched the requested address family.");
            return new StunNatBehaviorResult(
                StunNatBehaviorStatus.ResolveFailed,
                binding,
                StunNatMappingBehavior.Unknown,
                StunNatFilteringBehavior.Unknown,
                null,
                null,
                null,
                null,
                binding.Message);
        }

        try
        {
            await using var transport = StunUdpTransport.Create(serverEndPoint, options);
            var binding = await SendBindingTransactionAsync(
                transport,
                host,
                serverEndPoint,
                options,
                StunChangeRequest.None,
                cancellationToken).ConfigureAwait(false);

            if (binding.Status == StunBindingStatus.TimedOut)
            {
                return new StunNatBehaviorResult(
                    StunNatBehaviorStatus.UdpBlocked,
                    binding,
                    StunNatMappingBehavior.Unknown,
                    StunNatFilteringBehavior.Unknown,
                    null,
                    null,
                    null,
                    null,
                    "No response to the RFC 5389 Binding request.");
            }

            if (binding.Status != StunBindingStatus.Success)
            {
                return new StunNatBehaviorResult(
                    ToBehaviorStatus(binding.Status),
                    binding,
                    StunNatMappingBehavior.Unknown,
                    StunNatFilteringBehavior.Unknown,
                    null,
                    null,
                    null,
                    null,
                    binding.Message);
            }

            if (binding.ReflexiveEndPoint is null)
            {
                return new StunNatBehaviorResult(
                    StunNatBehaviorStatus.ProtocolError,
                    binding,
                    StunNatMappingBehavior.Unknown,
                    StunNatFilteringBehavior.Unknown,
                    null,
                    null,
                    null,
                    null,
                    "Binding response did not contain XOR-MAPPED-ADDRESS or MAPPED-ADDRESS.");
            }

            if (binding.OtherAddress is null)
            {
                return new StunNatBehaviorResult(
                    StunNatBehaviorStatus.Rfc5780NotSupported,
                    binding,
                    DetermineNoNatMapping(binding),
                    StunNatFilteringBehavior.Unknown,
                    null,
                    null,
                    null,
                    null,
                    "The server did not include RFC 5780 OTHER-ADDRESS.");
            }

            var filteringTest2 = await SendBindingTransactionAsync(
                transport,
                host,
                serverEndPoint,
                options,
                StunChangeRequest.ChangeIpAndPort,
                cancellationToken).ConfigureAwait(false);

            StunBindingResult? filteringTest3 = null;
            StunNatFilteringBehavior filteringBehavior;
            if (filteringTest2.Status == StunBindingStatus.Success)
            {
                filteringBehavior = StunNatFilteringBehavior.EndpointIndependent;
            }
            else if (filteringTest2.Status == StunBindingStatus.TimedOut)
            {
                filteringTest3 = await SendBindingTransactionAsync(
                    transport,
                    host,
                    serverEndPoint,
                    options,
                    StunChangeRequest.ChangePort,
                    cancellationToken).ConfigureAwait(false);

                filteringBehavior = filteringTest3.Status == StunBindingStatus.Success
                    ? StunNatFilteringBehavior.AddressDependent
                    : StunNatFilteringBehavior.AddressAndPortDependent;
            }
            else
            {
                return new StunNatBehaviorResult(
                    filteringTest2.Status == StunBindingStatus.ServerError
                        ? StunNatBehaviorStatus.Rfc5780NotSupported
                        : ToBehaviorStatus(filteringTest2.Status),
                    binding,
                    StunNatMappingBehavior.Unknown,
                    StunNatFilteringBehavior.Unknown,
                    null,
                    null,
                    filteringTest2,
                    null,
                    filteringTest2.Message ?? "Filtering test II failed.");
            }

            var mappingBehavior = DetermineNoNatMapping(binding);
            StunBindingResult? mappingTest2 = null;
            StunBindingResult? mappingTest3 = null;

            if (mappingBehavior != StunNatMappingBehavior.NotNated)
            {
                var alternateAddressPrimaryPort = new IPEndPoint(binding.OtherAddress.Address, serverEndPoint.Port);
                mappingTest2 = await SendBindingTransactionAsync(
                    transport,
                    host,
                    alternateAddressPrimaryPort,
                    options,
                    StunChangeRequest.None,
                    cancellationToken).ConfigureAwait(false);

                if (mappingTest2.Status == StunBindingStatus.Success && mappingTest2.ReflexiveEndPoint is not null)
                {
                    if (StunEndpointComparer.Equals(binding.ReflexiveEndPoint, mappingTest2.ReflexiveEndPoint))
                    {
                        mappingBehavior = StunNatMappingBehavior.EndpointIndependent;
                    }
                    else
                    {
                        mappingTest3 = await SendBindingTransactionAsync(
                            transport,
                            host,
                            binding.OtherAddress,
                            options,
                            StunChangeRequest.None,
                            cancellationToken).ConfigureAwait(false);

                        mappingBehavior = mappingTest3.Status == StunBindingStatus.Success
                            && mappingTest3.ReflexiveEndPoint is not null
                            && StunEndpointComparer.Equals(mappingTest2.ReflexiveEndPoint, mappingTest3.ReflexiveEndPoint)
                                ? StunNatMappingBehavior.AddressDependent
                                : StunNatMappingBehavior.AddressAndPortDependent;
                    }
                }
                else
                {
                    mappingBehavior = StunNatMappingBehavior.Unknown;
                }
            }

            return new StunNatBehaviorResult(
                StunNatBehaviorStatus.Success,
                binding,
                mappingBehavior,
                filteringBehavior,
                mappingTest2,
                mappingTest3,
                filteringTest2,
                filteringTest3,
                null);
        }
        catch (SocketException ex)
        {
            var binding = SocketErrorResult(host, serverEndPoint, null, ex);
            return new StunNatBehaviorResult(
                StunNatBehaviorStatus.SocketError,
                binding,
                StunNatMappingBehavior.Unknown,
                StunNatFilteringBehavior.Unknown,
                null,
                null,
                null,
                null,
                binding.Message);
        }
    }

    private static async ValueTask<StunBindingResult> SendBindingTransactionAsync(
        StunUdpTransport transport,
        string host,
        IPEndPoint remoteEndPoint,
        StunClientOptions options,
        StunChangeRequest changeRequest,
        CancellationToken cancellationToken)
    {
        var transactionId = new byte[StunConstants.TransactionIdLength];
        RandomNumberGenerator.Fill(transactionId);

        var request = new byte[512];
        var requestLength = StunMessageCodec.WriteBindingRequest(request, transactionId, changeRequest, options.Software);
        var receiveBuffer = new byte[options.ReceiveBufferSize];

        var timeout = options.InitialRto;
        string? lastProtocolError = null;

        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            var sendTimestamp = Stopwatch.GetTimestamp();
            await transport.SendToAsync(request.AsMemory(0, requestLength), remoteEndPoint, cancellationToken).ConfigureAwait(false);

            while (true)
            {
                var elapsed = Stopwatch.GetElapsedTime(sendTimestamp);
                var remaining = timeout - elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                var received = await transport.ReceiveAsync(receiveBuffer, remaining, cancellationToken).ConfigureAwait(false);
                if (!received.Received)
                {
                    break;
                }

                if (!StunMessageCodec.TryParse(receiveBuffer.AsSpan(0, received.Length), out var message, out var parseError))
                {
                    lastProtocolError = parseError;
                    continue;
                }

                if (message is null || !transactionId.AsSpan().SequenceEqual(message.TransactionId))
                {
                    continue;
                }

                var localEndPoint = transport.LocalEndPoint;
                var rtt = Stopwatch.GetElapsedTime(sendTimestamp);
                if (message.MessageType == StunConstants.BindingSuccessResponse)
                {
                    return new StunBindingResult(
                        StunBindingStatus.Success,
                        host,
                        remoteEndPoint,
                        localEndPoint,
                        message.ReflexiveEndPoint,
                        message.OtherAddress,
                        message.ResponseOrigin,
                        message.AlternateServer,
                        null,
                        rtt,
                        attempt,
                        null);
                }

                if (message.MessageType == StunConstants.BindingErrorResponse)
                {
                    return new StunBindingResult(
                        StunBindingStatus.ServerError,
                        host,
                        remoteEndPoint,
                        localEndPoint,
                        null,
                        message.OtherAddress,
                        message.ResponseOrigin,
                        message.AlternateServer,
                        message.Error,
                        rtt,
                        attempt,
                        message.Error is null
                            ? "Server returned a STUN error response."
                            : $"Server returned STUN error {message.Error.Code}: {message.Error.Reason}");
                }

                return new StunBindingResult(
                    StunBindingStatus.ProtocolError,
                    host,
                    remoteEndPoint,
                    localEndPoint,
                    null,
                    message.OtherAddress,
                    message.ResponseOrigin,
                    message.AlternateServer,
                    null,
                    rtt,
                    attempt,
                    $"Unexpected STUN message type 0x{message.MessageType:X4}.");
            }

            timeout = TimeSpan.FromTicks(timeout.Ticks * 2);
        }

        return new StunBindingResult(
            StunBindingStatus.TimedOut,
            host,
            remoteEndPoint,
            transport.LocalEndPoint,
            null,
            null,
            null,
            null,
            null,
            null,
            options.MaxAttempts,
            lastProtocolError is null
                ? "No matching STUN response was received."
                : $"No matching STUN response was received. Last parse error: {lastProtocolError}");
    }

    private static StunNatMappingBehavior DetermineNoNatMapping(StunBindingResult binding)
    {
        return binding.IsBehindNat == false
            ? StunNatMappingBehavior.NotNated
            : StunNatMappingBehavior.Unknown;
    }

    private static StunNatBehaviorStatus ToBehaviorStatus(StunBindingStatus status)
    {
        return status switch
        {
            StunBindingStatus.ResolveFailed => StunNatBehaviorStatus.ResolveFailed,
            StunBindingStatus.ProtocolError => StunNatBehaviorStatus.ProtocolError,
            StunBindingStatus.SocketError => StunNatBehaviorStatus.SocketError,
            _ => StunNatBehaviorStatus.ProtocolError
        };
    }

    private static StunBindingResult SocketErrorResult(string host, IPEndPoint? serverEndPoint, IPEndPoint? localEndPoint, SocketException ex)
    {
        return new StunBindingResult(
            StunBindingStatus.SocketError,
            host,
            serverEndPoint,
            localEndPoint,
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            $"Socket error {ex.SocketErrorCode}: {ex.Message}");
    }

    private static StunBindingResult ResolveFailedBindingResult(string host, SocketException ex)
    {
        return new StunBindingResult(
            StunBindingStatus.ResolveFailed,
            host,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            $"DNS resolve error {ex.SocketErrorCode}: {ex.Message}");
    }

    private static async ValueTask<(IPEndPoint? ServerEndPoint, SocketException? Error)> TryResolveAsync(
        string host,
        int port,
        StunClientOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await ResolveAsync(host, port, options, cancellationToken).ConfigureAwait(false), null);
        }
        catch (SocketException ex)
        {
            return (null, ex);
        }
    }

    private static async ValueTask<IPEndPoint?> ResolveAsync(
        string host,
        int port,
        StunClientOptions options,
        CancellationToken cancellationToken)
    {
        if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        if (IPAddress.TryParse(host, out var parsedAddress))
        {
            return AddressFamilyAllowed(parsedAddress.AddressFamily, options.AddressFamilyMode)
                ? new IPEndPoint(parsedAddress, port)
                : null;
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        return addresses
            .Where(address => AddressFamilyAllowed(address.AddressFamily, options.AddressFamilyMode))
            .OrderBy(address => AddressFamilyRank(address.AddressFamily, options.AddressFamilyMode))
            .Select(address => new IPEndPoint(address, port))
            .FirstOrDefault();
    }

    private static bool AddressFamilyAllowed(AddressFamily addressFamily, StunAddressFamilyMode mode)
    {
        return mode switch
        {
            StunAddressFamilyMode.Ipv4Only => addressFamily == AddressFamily.InterNetwork,
            StunAddressFamilyMode.Ipv6Only => addressFamily == AddressFamily.InterNetworkV6,
            _ => addressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6
        };
    }

    private static int AddressFamilyRank(AddressFamily addressFamily, StunAddressFamilyMode mode)
    {
        return mode switch
        {
            StunAddressFamilyMode.Ipv4Preferred => addressFamily == AddressFamily.InterNetwork ? 0 : 1,
            StunAddressFamilyMode.Ipv6Preferred => addressFamily == AddressFamily.InterNetworkV6 ? 0 : 1,
            _ => 0
        };
    }

    private sealed class StunUdpTransport : IAsyncDisposable
    {
        private readonly Socket _socket;
        private readonly EndPoint _receiveAny;

        private StunUdpTransport(Socket socket, EndPoint receiveAny)
        {
            _socket = socket;
            _receiveAny = receiveAny;
        }

        public IPEndPoint? LocalEndPoint => _socket.LocalEndPoint as IPEndPoint;

        public static StunUdpTransport Create(IPEndPoint remoteEndPoint, StunClientOptions options)
        {
            var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                if (options.LocalEndPoint is not null)
                {
                    socket.Bind(options.LocalEndPoint);
                }
                else
                {
                    var localAddress = TryGetLocalRouteAddress(remoteEndPoint) ?? AnyAddress(remoteEndPoint.AddressFamily);
                    socket.Bind(new IPEndPoint(localAddress, 0));
                }

                var receiveAny = remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Any, 0)
                    : new IPEndPoint(IPAddress.Any, 0);
                return new StunUdpTransport(socket, receiveAny);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        public async ValueTask SendToAsync(
            ReadOnlyMemory<byte> data,
            EndPoint remoteEndPoint,
            CancellationToken cancellationToken)
        {
            await _socket.SendToAsync(data, SocketFlags.None, remoteEndPoint, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<(bool Received, int Length, EndPoint? RemoteEndPoint)> ReceiveAsync(
            Memory<byte> buffer,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                var result = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, _receiveAny, timeoutCts.Token).ConfigureAwait(false);
                return (true, result.ReceivedBytes, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (false, 0, null);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _socket.Dispose();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        private static IPAddress? TryGetLocalRouteAddress(IPEndPoint remoteEndPoint)
        {
            try
            {
                using var probe = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                probe.Connect(remoteEndPoint);
                return (probe.LocalEndPoint as IPEndPoint)?.Address;
            }
            catch
            {
                return null;
            }
        }

        private static IPAddress AnyAddress(AddressFamily addressFamily)
        {
            return addressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
        }
    }
}
