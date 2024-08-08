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
    public static class NetworkHelper
    {

        /// <summary>
        /// 域名解析
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
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
                return Dns.GetHostEntry(domain, AddressFamily.InterNetwork).AddressList.FirstOrDefault();
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



        static List<string> starts = new() { "10.", "100.", "192.168.", "172." };
        public static ushort GetRouteLevel(string server, out List<IPAddress> result)
        {
            if (string.IsNullOrWhiteSpace(server) == false)
            {
                server = server.Split(':')[0];
            }

            if (OperatingSystem.IsWindows())
            {
                return GetRouteLevelWindows(server, out result);
            }
            return GetRouteLevelLinux(server, out result);
        }
        private static ushort GetRouteLevelLinux(string server, out List<IPAddress> result)
        {
            result = new List<IPAddress>();

            string str = CommandHelper.Linux(string.Empty, new string[] { $"traceroute {server} -4 -m 5" });
            string[] lines = str.Split(Environment.NewLine);

            Regex regex = new Regex(@"(\d+\.\d+\.\d+\.\d+)");
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
                    PingReply reply = pinger.Send(target, 100, Encoding.ASCII.GetBytes("snltty"), new PingOptions { Ttl = i, DontFragment = true });

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



        private static byte[] ipv6LocalBytes = new byte[] { 254, 128, 0, 0, 0, 0, 0, 0 };
        public static IPAddress[] GetIPV6()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                 .Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                 .Where(c => c.GetAddressBytes().AsSpan(0, 8).SequenceEqual(ipv6LocalBytes) == false).Distinct().ToArray();
            }
            catch (Exception)
            {
            }
            return Array.Empty<IPAddress>();
        }
        public static IPAddress[] GetIPV4()
        {
            try
            {
                return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Where(c => c.IsIPv4MappedToIPv6 == false)
                .Where(c => c.Equals(IPAddress.Loopback) == false)
                .Distinct().ToArray();
            }
            catch (Exception)
            {
            }
            return Array.Empty<IPAddress>();
        }

        public static byte MaskLength(uint ip)
        {
            byte maskLength = 32;
            for (int i = 0; i < sizeof(uint); i++)
            {
                if (((ip >> (i * 8)) & 0x000000ff) != 0)
                {
                    break;
                }
                maskLength -= 8;
            }
            return maskLength;
        }

        public static uint MaskValue(byte maskLength)
        {
            //最多<<31 所以0需要单独计算
            if (maskLength < 1) return 0;
            return 0xffffffff << (32 - maskLength);
        }
        public static IPAddress GetMaskIp(uint maskValue)
        {
            return new IPAddress(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(maskValue)));
        }
        public static IPAddress ToNetworkIp(IPAddress ip, uint maskvalue)
        {
            return ToNetworkIp(BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes()), maskvalue);
        }
        public static IPAddress ToNetworkIp(uint ip, uint maskvalue)
        {
            return new IPAddress(BinaryPrimitives.ReverseEndianness(ip & maskvalue).ToBytes());
        }
        public static IPAddress ToGatewayIP(IPAddress ip, byte maskLength)
        {
            uint network = BinaryPrimitives.ReadUInt32BigEndian(ToNetworkIp(ip, NetworkHelper.MaskValue(maskLength)).GetAddressBytes());
            IPAddress gateway = new IPAddress(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(network + 1)));
            return gateway;
        }
        public static IPAddress ToGatewayIP(uint ip, uint maskValue)
        {
            uint network = BinaryPrimitives.ReadUInt32BigEndian(ToNetworkIp(ip, maskValue).GetAddressBytes());
            IPAddress gateway = new IPAddress(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(network + 1)));
            return gateway;
        }

        public static bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (IPv6Support == false);
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

        public static bool IPv6Support = Socket.OSSupportsIPv6;
#endif
    }
}
