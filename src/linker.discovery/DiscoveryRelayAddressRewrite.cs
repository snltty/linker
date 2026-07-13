using System;
using System.Net;
using System.Net.Sockets;

namespace linker.discovery;

public sealed class DiscoveryAddressMap
{
    public IPAddress MappedNetwork { get; set; } = IPAddress.Any;

    public IPAddress RealNetwork { get; set; } = IPAddress.Any;

    public int PrefixLength { get; set; }

    public bool TryMapRealToMapped(IPAddress realAddress, out IPAddress mappedAddress)
    {
        ArgumentNullException.ThrowIfNull(realAddress);

        DiscoveryAddressMapEntry entry = DiscoveryAddressMapEntry.Create(this);
        return entry.TryMapRealToMapped(realAddress, out mappedAddress);
    }
}

internal readonly record struct DiscoveryAddressMapEntry(
    uint MappedNetwork,
    uint RealNetwork,
    uint Mask,
    int PrefixLength)
{
    public static DiscoveryAddressMapEntry Create(DiscoveryAddressMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        EnsureIPv4(map.MappedNetwork, nameof(map.MappedNetwork));
        EnsureIPv4(map.RealNetwork, nameof(map.RealNetwork));

        if (map.PrefixLength is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(map), "Address map prefix length must be between 0 and 32.");
        }

        uint mask = PrefixLengthToMask(map.PrefixLength);
        return new DiscoveryAddressMapEntry(
            ToUInt32(map.MappedNetwork) & mask,
            ToUInt32(map.RealNetwork) & mask,
            mask,
            map.PrefixLength);
    }

    public bool TryMapRealToMapped(IPAddress realAddress, out IPAddress mappedAddress)
    {
        ArgumentNullException.ThrowIfNull(realAddress);
        EnsureIPv4(realAddress, nameof(realAddress));

        Span<byte> realBytes = stackalloc byte[4];
        Span<byte> mappedBytes = stackalloc byte[4];
        if (!realAddress.TryWriteBytes(realBytes, out int written) ||
            written != 4 ||
            !TryMapRealToMapped(realBytes, mappedBytes))
        {
            mappedAddress = IPAddress.None;
            return false;
        }

        mappedAddress = new IPAddress(mappedBytes);
        return true;
    }

    public bool TryMapRealToMapped(ReadOnlySpan<byte> realAddress, Span<byte> mappedAddress)
    {
        if (realAddress.Length != 4 || mappedAddress.Length < 4)
        {
            return false;
        }

        uint real = ToUInt32(realAddress);
        if ((real & Mask) != RealNetwork)
        {
            return false;
        }

        WriteUInt32(mappedAddress, (MappedNetwork & Mask) | (real & ~Mask));
        return true;
    }

    public bool RealNetworkOverlaps(DiscoveryAddressMapEntry other)
    {
        return NetworksOverlap(RealNetwork, Mask, other.RealNetwork, other.Mask);
    }

    public bool MappedNetworkOverlaps(DiscoveryAddressMapEntry other)
    {
        return NetworksOverlap(MappedNetwork, Mask, other.MappedNetwork, other.Mask);
    }

    public IPAddress GetRealNetworkAddress()
    {
        return FromUInt32(RealNetwork);
    }

    public IPAddress GetMappedNetworkAddress()
    {
        return FromUInt32(MappedNetwork);
    }

    private static uint PrefixLengthToMask(int prefixLength)
    {
        return prefixLength == 0 ? 0 : uint.MaxValue << (32 - prefixLength);
    }

    private static bool NetworksOverlap(uint leftNetwork, uint leftMask, uint rightNetwork, uint rightMask)
    {
        return (leftNetwork & rightMask) == rightNetwork ||
            (rightNetwork & leftMask) == leftNetwork;
    }

    private static void EnsureIPv4(IPAddress address, string paramName)
    {
        ArgumentNullException.ThrowIfNull(address, paramName);

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Only IPv4 addresses are supported.", paramName);
        }
    }

    private static uint ToUInt32(IPAddress address)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (!address.TryWriteBytes(bytes, out int written) || written != 4)
        {
            throw new ArgumentException("Only IPv4 addresses are supported.", nameof(address));
        }

        return ToUInt32(bytes);
    }

    private static uint ToUInt32(ReadOnlySpan<byte> bytes)
    {
        return ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];
    }

    private static IPAddress FromUInt32(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        WriteUInt32(bytes, value);
        return new IPAddress(bytes);
    }

    private static void WriteUInt32(Span<byte> bytes, uint value)
    {
        bytes[0] = (byte)(value >> 24);
        bytes[1] = (byte)(value >> 16);
        bytes[2] = (byte)(value >> 8);
        bytes[3] = (byte)value;
    }
}

public sealed class DiscoveryRelayAddressRewrite
{
    public DiscoveryRelayAddressRewrite(
        DiscoveryProtocolInfo protocol,
        string direction,
        IPAddress originalAddress,
        IPAddress mappedAddress)
    {
        Protocol = protocol;
        Direction = direction;
        OriginalAddress = originalAddress;
        MappedAddress = mappedAddress;
    }

    public DiscoveryProtocolInfo Protocol { get; }

    public string Direction { get; }

    public IPAddress OriginalAddress { get; }

    public IPAddress MappedAddress { get; }
}
