using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace linker.discovery;

internal static class DiscoveryProtocolHelper
{
    public static void EnsureIPv4(IPAddress address, string paramName)
    {
        ArgumentNullException.ThrowIfNull(address, paramName);

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Only IPv4 addresses are supported.", paramName);
        }
    }

    public static bool IsAny(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return address.Equals(IPAddress.Any) || address.Equals(IPAddress.None);
    }

    public static bool IsMulticast(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        byte first = address.GetAddressBytes()[0];
        return first is >= 224 and <= 239;
    }

    public static bool IsLinkLocal(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        byte[] bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }

    public static IEnumerable<(NetworkInterface Adapter, IPAddress Address)> EnumerateUpIPv4Addresses()
    {
        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

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
                IPAddress address = unicast.Address;
                if (address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address) &&
                    !IsLinkLocal(address))
                {
                    yield return (adapter, address);
                }
            }
        }
    }

    public static int GetInterfaceIndex(IPAddress address)
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

    public static IPAddress GetBroadcastAddress(IPAddress address)
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

    public static IPv4Network GetNetworkRange(IPAddress address)
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

    public static IPEndPoint GetLanTarget(DiscoveryProtocolInfo protocol, IPAddress broadcastAddress)
    {
        if (protocol.Type == DiscoveryProtocolType.Broadcast &&
            (IsAny(protocol.Address) || protocol.Address.Equals(IPAddress.Broadcast)))
        {
            return new IPEndPoint(broadcastAddress, protocol.Port);
        }

        return new IPEndPoint(GetProtocolTargetAddress(protocol), protocol.Port);
    }

    private static IPAddress GetProtocolTargetAddress(DiscoveryProtocolInfo protocol)
    {
        if (protocol.Type == DiscoveryProtocolType.Broadcast && IsAny(protocol.Address))
        {
            return IPAddress.Broadcast;
        }

        return protocol.Address;
    }

    public readonly record struct IPv4Network(uint Address, uint Mask)
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
}
