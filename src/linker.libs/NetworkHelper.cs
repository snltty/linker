using linker.libs.extends;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace linker.libs
{

    public struct IPNetwork
    {
        public uint ip;
        public uint prefix;

        public IPNetwork(IPAddress ip, byte prefixLength)
        {
            this.ip = NetworkHelper.ToValue(ip);
            this.prefix = NetworkHelper.ToPrefixValue(prefixLength);
        }
        public IPNetwork(uint ip, uint prefix)
        {
            this.ip = ip;
            this.prefix = prefix;
        }

        public uint ToValue()
        {
            return NetworkHelper.ToNetworkValue(ip, prefix);
        }

        public bool Equals(IPNetwork other)
        {
            return ToValue() == other.ToValue();
        }
    }

    public static partial class NetworkHelper
    {
        public static IPEndPoint TransEndpointFamily(IPEndPoint ep)
        {
            if (ep.Address.AddressFamily == AddressFamily.InterNetworkV6 && ep.Address.IsIPv4MappedToIPv6)
            {
                Span<byte> bytes = stackalloc byte[16];
                ep.Address.TryWriteBytes(bytes, out _);
                return new IPEndPoint(new IPAddress(bytes[^4..]), ep.Port);
            }
            return ep;
        }

        public static ushort ApplyNewPort()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            ushort port = (ushort)(socket.LocalEndPoint as IPEndPoint).Port;

            socket.SafeClose();

            return port;
        }

        public static IPAddress GetDomainIp(string domain)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return null;
                }
                if (IPAddress.TryParse(domain, out IPAddress ip))
                {
                    return ip;
                }
                return Dns.GetHostEntry(domain).AddressList.FirstOrDefault();
            }
            catch (Exception)
            {
            }
            return null;
        }
        public static async Task<IPAddress> GetDomainIpAsync(string domain)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(domain))
                {
                    return null;
                }
                if (IPAddress.TryParse(domain, out IPAddress ip))
                {
                    return ip;
                }
                return (await Dns.GetHostEntryAsync(domain).ConfigureAwait(false)).AddressList.FirstOrDefault();
            }
            catch (Exception)
            {
            }
            return null;
        }
        public static IPEndPoint GetEndPoint(string host, int defaultPort)
        {
            try
            {
                string[] hostArr = host.Split(':');
                int port = defaultPort;
                if (hostArr.Length == 2)
                {
                    port = int.Parse(hostArr[1]);
                }
                IPAddress ip = GetDomainIp(hostArr[0]);
                return new IPEndPoint(ip, port);
            }
            catch (Exception)
            {
            }
            return null;
        }
        public static async Task<IPEndPoint> GetEndPointAsync(string host, int defaultPort)
        {
            try
            {
                string[] hostArr = host.Split(':');
                int port = defaultPort;
                string domain = hostArr[0];
                if (hostArr.Length > 1)
                {
                    port = int.Parse(hostArr[^1]);
                    domain = string.Join(":", hostArr.Take(hostArr.Length - 1));
                }
                IPAddress ip = await GetDomainIpAsync(domain).ConfigureAwait(false);
                return new IPEndPoint(ip, port);
            }
            catch (Exception)
            {
            }
            return null;
        }

        private readonly static List<string> starts = ["10.", "100.", "192.168.", "172."];
        public static ushort GetRouteLevel(string server, out List<IPAddress> result)
        {
            result = new List<IPAddress>();
            if (string.IsNullOrWhiteSpace(server) == false)
            {
                server = server.Split(':')[0];
            }

            if (OperatingSystem.IsWindows())
            {
                return GetRouteLevelWindows(server, out result);
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetRouteLevelLinux(server, out result);
            }
            return 3;
        }
        private static ushort GetRouteLevelLinux(string server, out List<IPAddress> result)
        {
            result = new List<IPAddress>();

            string str = CommandHelper.Linux(string.Empty, new string[] { $"traceroute {server} -4 -m 5 -w 1" });
            string[] lines = str.Split(Environment.NewLine);

            Regex regex = MyRegex();
            for (ushort i = 1; i < lines.Length; i++)
            {
                string ip = regex.Match(lines[i]).Groups[1].Value;
                if (starts.Any(c => ip.ToString().StartsWith(c)))
                {
                    result.Add(IPAddress.Parse(ip));
                }
                else
                {
                    return i;
                }
            }

            return 3;
        }
        private static ushort GetRouteLevelWindows(string server, out List<IPAddress> result)
        {
            result = new List<IPAddress>();
            try
            {
                IPAddress target = Dns.GetHostEntry(server).AddressList.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork);

                for (ushort i = 1; i <= 5; i++)
                {
                    using Ping pinger = new();
                    PingReply reply = pinger.Send(target, 100, Encoding.ASCII.GetBytes(Helper.GlobalString), new PingOptions { Ttl = i, DontFragment = true });
                    if (reply.Status != IPStatus.Success)
                    {
                        continue;
                    }

                    if (starts.Any(c => reply.Address.ToString().StartsWith(c)))
                    {
                        result.Add(reply.Address);
                    }
                    else
                    {
                        return i;
                    }
                }
            }
            catch (Exception)
            {
            }
            return 3;
        }


        private readonly static byte[] ipv6LocalBytes = [254, 128, 0, 0, 0, 0, 0, 0];

        private static IPAddress[] GetIP()
        {
            if (OperatingSystem.IsAndroid() == false)
            {
                try
                {
                    IPAddress[] ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    if (ips.Length > 0) return ips;
                }
                catch (Exception)
                {
                }
            }

            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .SelectMany(c => c.GetIPProperties().UnicastAddresses)
                    .Select(c => c.Address)
                    .ToArray();
            }
            catch (Exception)
            {
            }

            return Array.Empty<IPAddress>();
        }
        public static IPAddress[] GetIPV6()
        {
            return GetIP()
                 .Where(c => c.AddressFamily == AddressFamily.InterNetworkV6)
                 .Where(c => c.GetAddressBytes().AsSpan(0, 8).SequenceEqual(ipv6LocalBytes) == false)
                 .Where(c => c.Equals(IPAddress.IPv6Loopback) == false)
                 .Where(c =>
                 {
                     Span<byte> bytes = c.GetAddressBytes();
                     return (
                     bytes[0] == 0xFD
                     || (bytes[0] == 0xFE && (bytes[1] == 0x80 || bytes[1] == 0xC0))
                     ) == false;
                 })
                 .Distinct().ToArray();
        }
        public static IPAddress[] GetIPV4()
        {
            return GetIP()
                .Where(c => c.AddressFamily == AddressFamily.InterNetwork)
                .Where(c => c.IsIPv4MappedToIPv6 == false)
                .Where(c => c.Equals(IPAddress.Loopback) == false)
                .Distinct().ToArray();
        }

        public static byte ToPrefixLength(uint ip)
        {
            int maskLength = 32, i;
            for (i = 0; i < sizeof(uint) * 8; i++)
            {
                if (((ip >> i) & 0x01) != 0)
                {
                    break;
                }
            }
            return (byte)(maskLength - i);
        }
        public static uint ToPrefixValue(byte prefixLength)
        {
            //最多<<31 所以0需要单独计算
            if (prefixLength < 1) return 0;
            return 0xffffffff << (32 - prefixLength);
        }

        public static uint ToValue(IPAddress ip)
        {
            Span<byte> bytes = stackalloc byte[4];
            ip.TryWriteBytes(bytes, out _);
            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }
        public static uint ToValue(ReadOnlySpan<byte> span)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(span);
        }
        public static IPAddress ToIP(uint value)
        {
            return new IPAddress(BinaryPrimitives.ReverseEndianness(value));
        }
        public static IPAddress ToIP(ReadOnlySpan<byte> spsn)
        {
            return new IPAddress(spsn);
        }

        public static IPAddress ToNetworkIP(IPAddress ip, uint prefixIP)
        {
            return ToNetworkIP(ToValue(ip), prefixIP);
        }
        public static IPAddress ToNetworkIP(IPAddress ip, byte prefixLength)
        {
            return ToNetworkIP(ToValue(ip), ToPrefixValue(prefixLength));
        }
        public static IPAddress ToNetworkIP(uint ip, uint prefixIP)
        {
            return new IPAddress(BinaryPrimitives.ReverseEndianness(ToNetworkValue(ip, prefixIP)));
        }

        public static uint ToNetworkValue(IPAddress ip, byte prefixLength)
        {
            return ToNetworkValue(ToValue(ip), ToPrefixValue(prefixLength));
        }
        public static uint ToNetworkValue(uint ip, uint prefixIP)
        {
            return ip & prefixIP;
        }
        public static uint ToNetworkValue(uint ip, byte prefixLength)
        {
            return ToNetworkValue(ip, ToPrefixValue(prefixLength));
        }

        public static uint ToBroadcastValue(IPAddress ip, byte prefixLength)
        {
            return ToBroadcastValue(ToValue(ip), ToPrefixValue(prefixLength));
        }
        public static uint ToBroadcastValue(uint ip, uint prefixIP)
        {
            return ip | ~prefixIP;
        }

        public static IPAddress ToGatewayIP(IPAddress ip, byte prefixLength)
        {
            Span<byte> bytes = stackalloc byte[4];
            ToNetworkIP(ip, NetworkHelper.ToPrefixValue(prefixLength)).TryWriteBytes(bytes, out _);

            uint network = BinaryPrimitives.ReadUInt32BigEndian(bytes);
            IPAddress gateway = new IPAddress(BinaryPrimitives.ReverseEndianness(network + 1));
            return gateway;
        }
        public static IPAddress ToGatewayIP(uint ip, uint prefixIP)
        {
            Span<byte> bytes = stackalloc byte[4];
            ToNetworkIP(ip, prefixIP).TryWriteBytes(bytes, out _);
            uint network = BinaryPrimitives.ReadUInt32BigEndian(bytes);
            IPAddress gateway = new IPAddress(BinaryPrimitives.ReverseEndianness(network + 1));
            return gateway;
        }


#if DISABLE_IPV6 || (!UNITY_EDITOR && ENABLE_IL2CPP && !UNITY_2018_3_OR_NEWER)
            public static bool  IPv6Support = false;
#elif !UNITY_2019_1_OR_NEWER && !UNITY_2018_4_OR_NEWER && (!UNITY_EDITOR && ENABLE_IL2CPP && UNITY_2018_3_OR_NEWER)
           public static bool   IPv6Support = Socket.OSSupportsIPv6 && int.Parse(UnityEngine.Application.unityVersion.Remove(UnityEngine.Application.unityVersion.IndexOf('f')).Split('.')[2]) >= 6;
#elif UNITY_2018_2_OR_NEWER
           public static bool   IPv6Support = Socket.OSSupportsIPv6;
#elif UNITY
#pragma warning disable 618
           public static bool   IPv6Support = Socket.SupportsIPv6;
#pragma warning restore 618
#else

        public static bool IPv6Support => Socket.OSSupportsIPv6;

        [GeneratedRegex(@"(\d+\.\d+\.\d+\.\d+)")]
        private static partial Regex MyRegex();
#endif
    }
}
