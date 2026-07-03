using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace linker.tunnel;

public sealed class RadarTransfer : IDisposable
{
    private readonly object _gate = new();
    private readonly List<PublicEndpointSample> _samples = new();
    private readonly RadarOptions _options;

    private CancellationTokenSource? _probeCancellation;
    private Thread? _probeThread;
    private PortPredictionReport _currentReport = PortPredictionReport.Empty;
    private List<PublicEndpointSample>? _probeBatchSamples;
    private int _probeSampleCount;
    private bool _disposed;

    public RadarTransfer()
        : this(new RadarOptions())
    {
    }

    public RadarTransfer(RadarOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.MaxSamples < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxSamples must be greater than 1.");
        }

        if (_options.TargetSampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "TargetSampleCount cannot be negative.");
        }

        if (_options.TargetSampleCount > _options.MaxSamples)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "TargetSampleCount cannot be greater than MaxSamples.");
        }
    }

    public event EventHandler<IReadOnlyList<PublicEndpointSample>>? SamplesReceived;

    public void StartProbe()
    {
        ThrowIfDisposed();

        lock (_gate)
        {
            if (_probeThread is { IsAlive: true })
            {
                return;
            }

            var cancellation = new CancellationTokenSource();
            _probeCancellation = cancellation;
            _probeBatchSamples = new List<PublicEndpointSample>();
            _probeSampleCount = 0;
            _probeThread = new Thread(() => RunProbeLoop(cancellation))
            {
                IsBackground = true,
                Name = "linker.radar.stun-probe"
            };
            _probeThread.Start();
        }
    }

    public void StopProbe()
    {
        CancellationTokenSource? cancellation;
        Thread? thread;

        lock (_gate)
        {
            cancellation = _probeCancellation;
            thread = _probeThread;
            _probeCancellation = null;
            _probeThread = null;
            _probeBatchSamples = null;
            _probeSampleCount = 0;
        }

        cancellation?.Cancel();

        if (thread is not null && thread != Thread.CurrentThread)
        {
            thread.Join(_options.StopJoinTimeout);
        }

        cancellation?.Dispose();
    }

    public IReadOnlyList<int> Predict(int currentPublicPort, int maxCount = 400)
    {
        ThrowIfDisposed();
        ValidatePort(currentPublicPort, nameof(currentPublicPort));

        if (maxCount <= 0)
        {
            return Array.Empty<int>();
        }
        

        maxCount = Math.Min(maxCount, _options.MaxPredictionCount);

        PortPredictionReport report;
        PublicEndpointSample[] samples;

        lock (_gate)
        {
            report = _currentReport;
            samples = _samples.ToArray();
        }

        var candidates = new PortCandidateSet(maxCount);

        switch (report.Pattern)
        {
            case PortPattern.Stable:
                AddRing(candidates, currentPublicPort, _options.NearPortRadius);
                break;

            case PortPattern.Incremental:
            case PortPattern.Decremental:
            case PortPattern.FixedStep:
                AddStepPredictions(candidates, currentPublicPort, report.EstimatedStep);
                AddHotSegments(candidates, samples);
                break;

            case PortPattern.Clustered:
                AddRing(candidates, currentPublicPort, _options.NearPortRadius);
                AddHotSegments(candidates, samples);
                break;

            case PortPattern.RandomLike:
                AddRing(candidates, currentPublicPort, Math.Min(32, _options.NearPortRadius));
                AddHotSegments(candidates, samples);
                AddDistributedCandidates(candidates, currentPublicPort);
                break;

            case PortPattern.Unknown:
            default:
                AddRing(candidates, currentPublicPort, _options.NearPortRadius);
                AddHotSegments(candidates, samples);
                break;
        }

        return candidates.ToArray();
    }

    public PortPredictionReport ImportSamples(IEnumerable<PublicEndpointSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ThrowIfDisposed();

        var imported = samples.ToArray();
        foreach (var sample in imported)
        {
            ArgumentNullException.ThrowIfNull(sample);
            ValidatePort(sample.Port, $"{nameof(samples)}.{nameof(PublicEndpointSample.Port)}");
        }

        if (imported.Length == 0)
        {
            lock (_gate)
            {
                return _currentReport;
            }
        }

        PortPredictionReport report;

        lock (_gate)
        {
            _samples.Clear();

            foreach (var sample in imported)
            {
                _samples.Add(sample);
            }

            TrimSamplesUnsafe();
            report = Analyze(_samples, _currentReport);
            _currentReport = report;
        }

        return report;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopProbe();
        _disposed = true;
    }

    private void RunProbeLoop(CancellationTokenSource cancellation)
    {
        try
        {
            ProbeLoopAsync(cancellation.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            CompleteProbe(cancellation);
        }
    }

    private async Task ProbeLoopAsync(CancellationToken cancellationToken)
    {
        using var udp = CreateProbeSocket();

        while (!cancellationToken.IsCancellationRequested && !HasTargetSampleCount())
        {
            foreach (var server in _options.StunServers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HasTargetSampleCount())
                {
                    return;
                }

                var sample = await TryQueryStunAsync(udp, server, cancellationToken).ConfigureAwait(false);
                if (sample is not null)
                {
                    AddSample(sample);
                    AddProbeBatchSample(sample);
                    if (HasTargetSampleCount())
                    {
                        return;
                    }
                }

                await DelayIgnoreCancellation(_options.BetweenServerDelay, cancellationToken).ConfigureAwait(false);
            }

            await DelayIgnoreCancellation(_options.ProbeInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private void CompleteProbe(CancellationTokenSource cancellation)
    {
        var dispose = false;
        PublicEndpointSample[] batchSamples = [];

        lock (_gate)
        {
            if (ReferenceEquals(_probeCancellation, cancellation))
            {
                _probeCancellation = null;
                batchSamples = _probeBatchSamples?.ToArray() ?? [];
                _probeBatchSamples = null;
                dispose = true;
            }
        }

        if (dispose)
        {
            cancellation.Dispose();
        }

        if (batchSamples.Length > 0)
        {
            SamplesReceived?.Invoke(this, batchSamples);
        }

        lock (_gate)
        {
            if (Thread.CurrentThread == _probeThread)
            {
                _probeThread = null;
                _probeSampleCount = 0;
            }
        }
    }

    private bool HasTargetSampleCount()
    {
        lock (_gate)
        {
            return HasTargetSampleCountUnsafe();
        }
    }

    private bool HasTargetSampleCountUnsafe()
    {
        return _options.TargetSampleCount > 0 && _probeSampleCount >= _options.TargetSampleCount;
    }

    private void AddProbeBatchSample(PublicEndpointSample sample)
    {
        lock (_gate)
        {
            _probeSampleCount++;
            _probeBatchSamples?.Add(sample);
        }
    }

    private UdpClient CreateProbeSocket()
    {
        var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, _options.LocalProbePort));
        return udp;
    }

    private async Task<PublicEndpointSample?> TryQueryStunAsync(
        UdpClient udp,
        StunServer server,
        CancellationToken cancellationToken)
    {
        IPEndPoint? remote;

        try
        {
            remote = await ResolveServerAsync(server, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            return null;
        }

        if (remote is null)
        {
            return null;
        }

        var transactionId = new byte[12];
        RandomNumberGenerator.Fill(transactionId);

        var request = StunMessage.CreateBindingRequest(transactionId);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.StunTimeout);

        try
        {
            await udp.SendAsync(request, request.Length, remote).WaitAsync(timeout.Token).ConfigureAwait(false);

            while (!timeout.Token.IsCancellationRequested)
            {
                var receive = await udp.ReceiveAsync(timeout.Token).ConfigureAwait(false);
                if (!StunMessage.TryReadMappedEndpoint(receive.Buffer, transactionId, out var mapped))
                {
                    continue;
                }

                return new PublicEndpointSample(
                    mapped.Address,
                    mapped.Port,
                    server.ToString(),
                    DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }

        return null;
    }

    private static async Task<IPEndPoint?> ResolveServerAsync(StunServer server, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(server.Host, out var parsed))
        {
            return new IPEndPoint(parsed, server.Port);
        }

        var addresses = await Dns.GetHostAddressesAsync(server.Host, cancellationToken).ConfigureAwait(false);
        var address = addresses.FirstOrDefault(static item => item.AddressFamily == AddressFamily.InterNetwork);
        return address is null ? null : new IPEndPoint(address, server.Port);
    }

    private PortPredictionReport AddSample(PublicEndpointSample sample)
    {
        PortPredictionReport report;

        lock (_gate)
        {
            _samples.Add(sample);

            TrimSamplesUnsafe();
            report = Analyze(_samples, _currentReport);
            _currentReport = report;
        }

        return report;
    }

    private void TrimSamplesUnsafe()
    {
        while (_samples.Count > _options.MaxSamples)
        {
            _samples.RemoveAt(0);
        }
    }

    private static PortPredictionReport Analyze(
        IReadOnlyList<PublicEndpointSample> samples,
        PortPredictionReport? fallbackReport = null)
    {
        if (samples.Count == 0)
        {
            return PortPredictionReport.Empty;
        }

        if (fallbackReport is { SampleCount: > 0, LastPublicAddress: { } fallbackAddress })
        {
            var latestAddress = samples[^1].Address;
            if (!fallbackAddress.Equals(latestAddress))
            {
                var latestAddressSampleCount = samples.Count(sample => sample.Address.Equals(latestAddress));
                if (latestAddressSampleCount < 3)
                {
                    return fallbackReport;
                }
            }
        }

        var analysisSamples = SelectAnalysisSamples(samples, fallbackReport);
        var ports = analysisSamples.Select(sample => sample.Port).ToArray();
        var distinctPorts = ports.Distinct().ToArray();
        var last = analysisSamples[^1];

        if (ports.Length < 3)
        {
            return new PortPredictionReport(
                PortPattern.Unknown,
                ports.Length,
                last.Address,
                last.Port,
                0,
                0.2,
                new ReadOnlyCollection<int>(ports));
        }

        if (distinctPorts.Length == 1)
        {
            return new PortPredictionReport(
                PortPattern.Stable,
                ports.Length,
                last.Address,
                last.Port,
                0,
                1.0,
                new ReadOnlyCollection<int>(ports));
        }

        var deltas = new List<int>(ports.Length - 1);
        for (var i = 1; i < ports.Length; i++)
        {
            deltas.Add(NormalizePortDelta(ports[i] - ports[i - 1]));
        }

        var nonZeroDeltas = deltas.Where(delta => delta != 0).ToArray();
        if (nonZeroDeltas.Length == 0)
        {
            return new PortPredictionReport(
                PortPattern.Stable,
                ports.Length,
                last.Address,
                last.Port,
                0,
                0.95,
                new ReadOnlyCollection<int>(ports));
        }

        var estimatedStep = RoundMedian(nonZeroDeltas);
        var sameStepCount = nonZeroDeltas.Count(delta => Math.Abs(delta - estimatedStep) <= 1);
        var sameDirectionCount = nonZeroDeltas.Count(delta => Math.Sign(delta) == Math.Sign(estimatedStep));
        var sameStepRatio = sameStepCount / (double)nonZeroDeltas.Length;
        var sameDirectionRatio = sameDirectionCount / (double)nonZeroDeltas.Length;
        var portRange = distinctPorts.Max() - distinctPorts.Min();

        if (sameStepRatio >= 0.75 && Math.Abs(estimatedStep) <= 4096)
        {
            var pattern = estimatedStep switch
            {
                1 => PortPattern.Incremental,
                -1 => PortPattern.Decremental,
                > 0 => PortPattern.FixedStep,
                < 0 => PortPattern.FixedStep,
                _ => PortPattern.Unknown
            };

            return new PortPredictionReport(
                pattern,
                ports.Length,
                last.Address,
                last.Port,
                estimatedStep,
                Math.Min(0.95, 0.55 + sameStepRatio * 0.4),
                new ReadOnlyCollection<int>(ports));
        }

        if (sameDirectionRatio >= 0.75 && Math.Abs(estimatedStep) <= 4096)
        {
            return new PortPredictionReport(
                estimatedStep > 0 ? PortPattern.Incremental : PortPattern.Decremental,
                ports.Length,
                last.Address,
                last.Port,
                estimatedStep,
                Math.Min(0.8, 0.35 + sameDirectionRatio * 0.4),
                new ReadOnlyCollection<int>(ports));
        }

        if (portRange <= 2048 || distinctPorts.Length <= Math.Max(3, ports.Length / 2))
        {
            return new PortPredictionReport(
                PortPattern.Clustered,
                ports.Length,
                last.Address,
                last.Port,
                estimatedStep,
                0.45,
                new ReadOnlyCollection<int>(ports));
        }

        return new PortPredictionReport(
            PortPattern.RandomLike,
            ports.Length,
            last.Address,
            last.Port,
            estimatedStep,
            0.15,
            new ReadOnlyCollection<int>(ports));
    }

    private static IReadOnlyList<PublicEndpointSample> SelectAnalysisSamples(
        IReadOnlyList<PublicEndpointSample> samples,
        PortPredictionReport? fallbackReport)
    {
        var latestAddress = samples[^1].Address;
        var latestAddressSamples = samples
            .Where(sample => sample.Address.Equals(latestAddress))
            .ToArray();

        if (latestAddressSamples.Length >= 3 || fallbackReport is null or { SampleCount: 0 })
        {
            return latestAddressSamples;
        }

        if (fallbackReport.LastPublicAddress is { } fallbackAddress)
        {
            var fallbackAddressSamples = samples
                .Where(sample => sample.Address.Equals(fallbackAddress))
                .ToArray();

            if (fallbackAddressSamples.Length > 0)
            {
                return fallbackAddressSamples;
            }
        }

        return latestAddressSamples;
    }

    private void AddStepPredictions(PortCandidateSet candidates, int currentPublicPort, int estimatedStep)
    {
        var step = estimatedStep == 0 ? 1 : estimatedStep;
        var direction = Math.Sign(step);

        AddStepRing(candidates, currentPublicPort, step, _options.NearPortRadius);
        AddRing(candidates, WrapPort(currentPublicPort + step), Math.Min(16, _options.NearPortRadius));
        AddRing(candidates, currentPublicPort, Math.Min(16, _options.NearPortRadius));
        AddDirectionalSweep(candidates, currentPublicPort, direction, _options.NearPortRadius);
    }

    private void AddHotSegments(PortCandidateSet candidates, IReadOnlyList<PublicEndpointSample> samples)
    {
        foreach (var port in samples
                     .Select(sample => sample.Port)
                     .Reverse()
                     .Distinct()
                     .Take(_options.HotSegmentCount))
        {
            AddRing(candidates, port, _options.SegmentPortRadius);
            if (candidates.IsFull)
            {
                return;
            }
        }
    }

    private void AddDistributedCandidates(PortCandidateSet candidates, int currentPublicPort)
    {
        var min = Math.Clamp(_options.DistributedPortMin, 1, 65535);
        var max = Math.Clamp(_options.DistributedPortMax, min, 65535);
        var width = max - min + 1;
        var stride = Math.Max(1, _options.DistributedStride);
        var seed = unchecked((uint)(currentPublicPort * 1103515245 + 12345));
        var start = (int)(seed % (uint)width);

        for (var i = 0; i < width && !candidates.IsFull; i += stride)
        {
            candidates.Add(min + ((start + i) % width));
        }
    }

    private static void AddRing(PortCandidateSet candidates, int centerPort, int radius)
    {
        candidates.Add(centerPort);

        for (var offset = 1; offset <= radius && !candidates.IsFull; offset++)
        {
            candidates.Add(WrapPort(centerPort + offset));
            candidates.Add(WrapPort(centerPort - offset));
        }
    }

    private static void AddStepRing(PortCandidateSet candidates, int centerPort, int step, int radius)
    {
        var normalizedStep = step == 0 ? 1 : step;
        var maxOffset = Math.Max(1, radius / Math.Max(1, Math.Abs(normalizedStep)));

        candidates.Add(WrapPort(centerPort + normalizedStep));
        candidates.Add(centerPort);
        candidates.Add(WrapPort(centerPort - normalizedStep));

        for (var offset = 2; offset <= maxOffset && !candidates.IsFull; offset++)
        {
            candidates.Add(WrapPort(centerPort + normalizedStep * offset));
            candidates.Add(WrapPort(centerPort - normalizedStep * offset));
        }
    }

    private static void AddDirectionalSweep(PortCandidateSet candidates, int currentPublicPort, int direction, int distance)
    {
        if (direction == 0)
        {
            direction = 1;
        }

        for (var offset = 1; offset <= distance && !candidates.IsFull; offset++)
        {
            candidates.Add(WrapPort(currentPublicPort + offset * direction));
        }
    }

    private static int NormalizePortDelta(int delta)
    {
        const int portCount = 65535;

        if (delta > portCount / 2)
        {
            return delta - portCount;
        }

        if (delta < -portCount / 2)
        {
            return delta + portCount;
        }

        return delta;
    }

    private static int RoundMedian(IReadOnlyList<int> values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;

        if (sorted.Length % 2 == 1)
        {
            return sorted[middle];
        }

        return (int)Math.Round((sorted[middle - 1] + sorted[middle]) / 2.0, MidpointRounding.AwayFromZero);
    }

    private static int WrapPort(int port)
    {
        const int minPort = 1;
        const int maxPort = 65535;
        const int portCount = maxPort - minPort + 1;

        var normalized = (port - minPort) % portCount;
        if (normalized < 0)
        {
            normalized += portCount;
        }

        return normalized + minPort;
    }

    private static void ValidatePort(int port, string argumentName)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(argumentName, "Port must be in the range 1-65535.");
        }
    }

    private static async Task DelayIgnoreCancellation(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

public sealed record RadarOptions
{
    public IReadOnlyList<StunServer> StunServers { get; init; } = StunServer.Defaults;

    public TimeSpan ProbeInterval { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan BetweenServerDelay { get; init; } = TimeSpan.FromMilliseconds(120);

    public TimeSpan StunTimeout { get; init; } = TimeSpan.FromMilliseconds(1200);

    public TimeSpan StopJoinTimeout { get; init; } = TimeSpan.FromSeconds(2);

    public int LocalProbePort { get; init; }

    public int MaxSamples { get; init; } = 64;

    public int TargetSampleCount { get; init; } = 10;

    public int MaxPredictionCount { get; init; } = 1024;

    public int NearPortRadius { get; init; } = 128;

    public int SegmentPortRadius { get; init; } = 64;

    public int HotSegmentCount { get; init; } = 6;

    public int DistributedPortMin { get; init; } = 10000;

    public int DistributedPortMax { get; init; } = 60000;

    public int DistributedStride { get; init; } = 251;
}

public sealed record StunServer(string Host, int Port)
{
    public static IReadOnlyList<StunServer> Defaults { get; } =
    [
        new("stun.l.google.com", 19302),
        new("stun1.l.google.com", 19302),
        new("stun2.l.google.com", 19302),
        new("stun3.l.google.com", 19302),
        new("stun4.l.google.com", 19302),
        new("stun.cloudflare.com", 3478),
        new("global.stun.twilio.com", 3478),
        new("stun.nextcloud.com", 443),
        new("stun.stunprotocol.org", 3478),
        new("stun.linphone.org", 3478),
        new("stun.pjsip.org", 3478),
        new("stun.freeswitch.org", 3478),
        new("stun.antisip.com", 3478),
        new("stun.sipgate.net", 3478),
        new("stun.1und1.de", 3478),
        new("stun.gmx.de", 3478),
        new("stun.3cx.com", 3478)
    ];

    public override string ToString() => $"{Host}:{Port}";
}

public sealed record PublicEndpointSample(
    IPAddress Address,
    int Port,
    string Source,
    DateTimeOffset Timestamp);

public sealed record PortPredictionReport(
    PortPattern Pattern,
    int SampleCount,
    IPAddress? LastPublicAddress,
    int? LastPublicPort,
    int EstimatedStep,
    double Confidence,
    IReadOnlyList<int> ObservedPorts)
{
    public static PortPredictionReport Empty { get; } = new(
        PortPattern.Unknown,
        0,
        null,
        null,
        0,
        0,
        Array.Empty<int>());
}

public enum PortPattern
{
    Unknown = 0,
    Stable = 1,
    Incremental = 2,
    Decremental = 3,
    FixedStep = 4,
    Clustered = 5,
    RandomLike = 6
}

internal sealed class PortCandidateSet
{
    private readonly int _maxCount;
    private readonly List<int> _ports = new();
    private readonly HashSet<int> _seen = new();

    public PortCandidateSet(int maxCount)
    {
        _maxCount = maxCount;
    }

    public bool IsFull => _ports.Count >= _maxCount;

    public void Add(int port)
    {
        if (IsFull || port is < 1 or > 65535 || !_seen.Add(port))
        {
            return;
        }

        _ports.Add(port);
    }

    public int[] ToArray() => _ports.ToArray();
}

internal static class StunMessage
{
    private const ushort BindingRequest = 0x0001;
    private const ushort BindingSuccessResponse = 0x0101;
    private const ushort MappedAddress = 0x0001;
    private const ushort XorMappedAddress = 0x0020;
    private const uint MagicCookie = 0x2112A442;

    public static byte[] CreateBindingRequest(ReadOnlySpan<byte> transactionId)
    {
        if (transactionId.Length != 12)
        {
            throw new ArgumentException("STUN transaction id must be 12 bytes.", nameof(transactionId));
        }

        var request = new byte[20];
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(0, 2), BindingRequest);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(2, 2), 0);
        BinaryPrimitives.WriteUInt32BigEndian(request.AsSpan(4, 4), MagicCookie);
        transactionId.CopyTo(request.AsSpan(8, 12));
        return request;
    }

    public static bool TryReadMappedEndpoint(
        ReadOnlySpan<byte> buffer,
        ReadOnlySpan<byte> transactionId,
        out IPEndPoint endpoint)
    {
        endpoint = null!;

        if (buffer.Length < 20 || transactionId.Length != 12)
        {
            return false;
        }

        var messageType = BinaryPrimitives.ReadUInt16BigEndian(buffer[..2]);
        if (messageType != BindingSuccessResponse)
        {
            return false;
        }

        var messageLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2, 2));
        if (buffer.Length < 20 + messageLength)
        {
            return false;
        }

        var cookie = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4));
        if (cookie != MagicCookie || !buffer.Slice(8, 12).SequenceEqual(transactionId))
        {
            return false;
        }

        var attributesEnd = 20 + messageLength;
        var offset = 20;

        while (offset + 4 <= attributesEnd)
        {
            var attributeType = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
            var attributeLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 2, 2));
            offset += 4;

            if (offset + attributeLength > attributesEnd)
            {
                return false;
            }

            var value = buffer.Slice(offset, attributeLength);
            if (attributeType == XorMappedAddress && TryReadXorMappedAddress(value, transactionId, out endpoint))
            {
                return true;
            }

            if (attributeType == MappedAddress && TryReadMappedAddress(value, out endpoint))
            {
                return true;
            }

            offset += AlignToFour(attributeLength);
        }

        return false;
    }

    private static bool TryReadXorMappedAddress(
        ReadOnlySpan<byte> value,
        ReadOnlySpan<byte> transactionId,
        out IPEndPoint endpoint)
    {
        endpoint = null!;

        if (value.Length < 8)
        {
            return false;
        }

        var family = value[1];
        var port = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2, 2)) ^ (int)(MagicCookie >> 16);

        if (family == 0x01)
        {
            var addressValue = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(4, 4)) ^ MagicCookie;
            var addressBytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(addressBytes, addressValue);
            endpoint = new IPEndPoint(new IPAddress(addressBytes), port);
            return true;
        }

        if (family == 0x02 && value.Length >= 20)
        {
            Span<byte> mask = stackalloc byte[16];
            BinaryPrimitives.WriteUInt32BigEndian(mask[..4], MagicCookie);
            transactionId.CopyTo(mask[4..]);

            var addressBytes = new byte[16];
            for (var i = 0; i < 16; i++)
            {
                addressBytes[i] = (byte)(value[4 + i] ^ mask[i]);
            }

            endpoint = new IPEndPoint(new IPAddress(addressBytes), port);
            return true;
        }

        return false;
    }

    private static bool TryReadMappedAddress(ReadOnlySpan<byte> value, out IPEndPoint endpoint)
    {
        endpoint = null!;

        if (value.Length < 8)
        {
            return false;
        }

        var family = value[1];
        var port = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2, 2));

        if (family == 0x01)
        {
            endpoint = new IPEndPoint(new IPAddress(value.Slice(4, 4)), port);
            return true;
        }

        if (family == 0x02 && value.Length >= 20)
        {
            endpoint = new IPEndPoint(new IPAddress(value.Slice(4, 16)), port);
            return true;
        }

        return false;
    }

    private static int AlignToFour(int length)
    {
        var remainder = length % 4;
        return remainder == 0 ? length : length + 4 - remainder;
    }
}
