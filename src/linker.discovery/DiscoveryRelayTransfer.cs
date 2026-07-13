using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace linker.discovery;

public sealed class DiscoveryRelayTransfer : IDisposable
{
   
    private readonly object _lifecycleGate = new();
    private readonly object _gate = new();
    private List<DiscoveryRelaySession> _sessions = [];
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event Action<DiscoveryRelayError>? Error;

    public event Action<DiscoveryRelayAddressRewrite>? AddressRewrite;

    public event Action<DiscoveryRelayPayloadRewrite>? PayloadRewrite;

    public event Action<DiscoveryRelayPacketTrace>? PacketTrace;

    public static List<IPAddress> GetLanIps()
    {
        return ResolveLanIps(null, null);
    }

    public void StartRelay(IPAddress tunIp, List<DiscoveryProtocolInfo> protocols)
    {
        StartRelay(tunIp, protocols, null);
    }

    public void StartRelay(IPAddress tunIp, List<DiscoveryProtocolInfo> protocols, List<DiscoveryAddressMap>? maps)
    {
        ArgumentNullException.ThrowIfNull(tunIp);
        ArgumentNullException.ThrowIfNull(protocols);

        lock (_lifecycleGate)
        {
            StopRelayCore();

            List<DiscoveryProtocolInfo> enabledProtocols = PrepareProtocols(protocols);
            if (enabledProtocols.Count == 0)
            {
                return;
            }

            IPAddress resolvedTunIp = ResolveTunIp(tunIp);
            List<DiscoveryAddressMapEntry> addressMaps = PrepareAddressMaps(maps);
            ValidateUniqueEnabledPorts(enabledProtocols);

            var cts = new CancellationTokenSource();
            var sessions = new List<DiscoveryRelaySession>(enabledProtocols.Count);

            try
            {
                foreach (DiscoveryProtocolInfo protocol in enabledProtocols)
                {
                    List<IPAddress> resolvedLanIps = ResolveLanIps(protocol.LanIps, resolvedTunIp);
                    if (resolvedLanIps.Count == 0)
                    {
                        throw new InvalidOperationException($"No usable LAN IPv4 address was found for discovery protocol '{protocol.Name}'.");
                    }

                    sessions.Add(new DiscoveryRelaySession(
                        protocol,
                        resolvedTunIp,
                        resolvedLanIps,
                        addressMaps,
                        RaiseError,
                        RaiseAddressRewrite,
                        RaisePayloadRewrite,
                        RaisePacketTrace));
                }
            }
            catch
            {
                foreach (DiscoveryRelaySession session in sessions)
                {
                    session.Dispose();
                }

                cts.Dispose();
                throw;
            }

            lock (_gate)
            {
                if (_disposed)
                {
                    foreach (DiscoveryRelaySession session in sessions)
                    {
                        session.Dispose();
                    }

                    cts.Dispose();
                    throw new ObjectDisposedException(nameof(DiscoveryRelayTransfer));
                }

                _cts = cts;
                _sessions = sessions;

                foreach (DiscoveryRelaySession session in sessions)
                {
                    session.Start(cts.Token);
                }
            }
        }
    }

    public void StopRelay()
    {
        lock (_lifecycleGate)
        {
            StopRelayCore();
        }
    }

    private void StopRelayCore()
    {
        CancellationTokenSource? cts;
        List<DiscoveryRelaySession> sessions;

        lock (_gate)
        {
            cts = _cts;
            sessions = _sessions;
            _cts = null;
            _sessions = [];
        }

        if (cts is null)
        {
            return;
        }

        cts.Cancel();

        var tasks = new List<Task>(sessions.Count * 2);
        foreach (DiscoveryRelaySession session in sessions)
        {
            session.AddTasks(tasks);
            session.Dispose();
        }

        try
        {
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        cts.Dispose();
    }

    public void Dispose()
    {
        lock (_lifecycleGate)
        {
            StopRelayCore();
            lock (_gate)
            {
                _disposed = true;
            }
        }
    }

    private static void ValidateProtocol(DiscoveryProtocolInfo protocol)
    {
        DiscoveryProtocolHelper.EnsureIPv4(protocol.Address, nameof(protocol.Address));

        if (protocol.Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(nameof(protocol), "Protocol port must be between 0 and 65535.");
        }

        if (protocol.Ttl is < 1 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(protocol), "Protocol TTL must be between 1 and 255.");
        }

        if (protocol.Type is not DiscoveryProtocolType.Multicast and not DiscoveryProtocolType.Broadcast)
        {
            throw new ArgumentOutOfRangeException(nameof(protocol), "Protocol type must be Multicast or Broadcast.");
        }

        if (protocol.Type == DiscoveryProtocolType.Multicast && !DiscoveryProtocolHelper.IsMulticast(protocol.Address))
        {
            throw new ArgumentException("Multicast protocols require a multicast IPv4 address.", nameof(protocol));
        }
    }

    private List<DiscoveryProtocolInfo> PrepareProtocols(List<DiscoveryProtocolInfo> protocols)
    {
        var next = new List<DiscoveryProtocolInfo>(protocols.Count);
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (DiscoveryProtocolInfo protocol in protocols)
            {
                ArgumentNullException.ThrowIfNull(protocol);

                DiscoveryProtocolInfo clone = protocol.Clone();
                ValidateProtocol(clone);
                if (clone.Disabled == false)
                {
                    next.Add(clone);
                }
            }
        }

        return next;
    }

    private static void ValidateUniqueEnabledPorts(List<DiscoveryProtocolInfo> protocols)
    {
        var usedPorts = new HashSet<int>();
        foreach (DiscoveryProtocolInfo protocol in protocols)
        {
            if (!usedPorts.Add(protocol.Port))
            {
                throw new InvalidOperationException($"Multiple enabled discovery protocols use UDP port {protocol.Port}.");
            }
        }
    }

    internal static List<DiscoveryAddressMapEntry> PrepareAddressMaps(List<DiscoveryAddressMap>? maps)
    {
        var result = new List<DiscoveryAddressMapEntry>(maps?.Count ?? 0);
        if (maps is null)
        {
            return result;
        }

        foreach (DiscoveryAddressMap map in maps)
        {
            DiscoveryAddressMapEntry entry = DiscoveryAddressMapEntry.Create(map);
            bool overlapsAcceptedMap = false;
            foreach (DiscoveryAddressMapEntry accepted in result)
            {
                if (entry.RealNetworkOverlaps(accepted) || entry.MappedNetworkOverlaps(accepted))
                {
                    overlapsAcceptedMap = true;
                    break;
                }
            }

            if (!overlapsAcceptedMap)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static IPAddress ResolveTunIp(IPAddress tunIp)
    {
        DiscoveryProtocolHelper.EnsureIPv4(tunIp, nameof(tunIp));
        if (DiscoveryProtocolHelper.IsAny(tunIp))
        {
            throw new ArgumentException("TUN IPv4 address must be explicitly specified and cannot be 0.0.0.0.", nameof(tunIp));
        }

        return tunIp;
    }

    private static List<IPAddress> ResolveLanIps(List<IPAddress>? lanIps, IPAddress? tunIp)
    {
        if (lanIps is { Count: > 0 } && !lanIps.Any(DiscoveryProtocolHelper.IsAny))
        {
            var selected = new List<IPAddress>(lanIps.Count);
            foreach (IPAddress lanIp in lanIps)
            {
                DiscoveryProtocolHelper.EnsureIPv4(lanIp, nameof(lanIps));
                if ((tunIp is null || !lanIp.Equals(tunIp)) && !selected.Contains(lanIp))
                {
                    selected.Add(lanIp);
                }
            }

            if (selected.Count > 0)
            {
                return selected;
            }
        }

        var result = new List<IPAddress>();
        foreach ((_, IPAddress address) in DiscoveryProtocolHelper.EnumerateUpIPv4Addresses())
        {
            if ((tunIp is null || !address.Equals(tunIp)) && !result.Contains(address))
            {
                result.Add(address);
            }
        }

        return result;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
    private void RaiseError(DiscoveryRelayError error)
    {
        try
        {
            Error?.Invoke(error);
        }
        catch
        {
        }
    }

    private void RaiseAddressRewrite(DiscoveryRelayAddressRewrite rewrite)
    {
        try
        {
            AddressRewrite?.Invoke(rewrite);
        }
        catch
        {
        }
    }

    private void RaisePayloadRewrite(DiscoveryRelayPayloadRewrite rewrite)
    {
        try
        {
            PayloadRewrite?.Invoke(rewrite);
        }
        catch
        {
        }
    }

    private void RaisePacketTrace(DiscoveryRelayPacketTrace trace)
    {
        try
        {
            PacketTrace?.Invoke(trace);
        }
        catch
        {
        }
    }

}
