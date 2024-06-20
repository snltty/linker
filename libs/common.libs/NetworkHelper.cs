using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace common.libs
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
                IPAddress[] ips = Dns.GetHostEntry(domain).AddressList;
                ip = ips.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork);
                if (ip == null)
                {
                    ip = ips.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetworkV6);
                }
                return ip;
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static IPEndPoint GetEndPoint(string host, int defaultPort)
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

        public static ushort GetRouteLevel(out List<IPAddress> ips)
        {
            ips = new List<IPAddress>();
            try
            {
                List<string> starts = new() { "10.", "100.", "192.168.", "172." };
                var list = GetTraceRoute("www.baidu.com").ToList();
                for (ushort i = 0; i < list.Count(); i++)
                {
                    string ip = list.ElementAt(i).ToString();
                    if (starts.Any(c => ip.StartsWith(c)))
                    {
                        ips.Add(list.ElementAt(i));
                    }
                    else
                    {
                        if (i <= 2) return 3;
                        return (ushort)(i + 1);
                    }
                }
            }
            catch (Exception)
            {
            }
            return 3;
        }
        public static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress)
        {
            return GetTraceRoute(hostNameOrAddress, 1);
        }
        private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl)
        {
            IPAddress target = Dns.GetHostEntry(hostNameOrAddress).AddressList.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork);

            using Ping pinger = new();
            // 创建PingOptions对象
            PingOptions pingerOptions = new(ttl, true);
            int timeout = 100;
            byte[] buffer = Encoding.ASCII.GetBytes("11");
            // 创建PingReply对象
            // 发送ping命令
            PingReply reply = pinger.Send(target, timeout, buffer, pingerOptions);

            // 处理返回结果
            List<IPAddress> result = new();
            if (reply.Status == IPStatus.Success)
            {
                result.Add(reply.Address);
            }
            else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
            {
                //增加当前这个访问地址
                if (reply.Status == IPStatus.TtlExpired)
                {
                    result.Add(reply.Address);
                }

                if (ttl <= 10)
                {
                    //递归访问下一个地址
                    IEnumerable<IPAddress> tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1);
                    result.AddRange(tempResult);
                }
            }
            else
            {
                //失败
            }
            return result;
        }


        private static byte[] ipv6LocalBytes = new byte[] { 254, 128, 0, 0, 0, 0, 0, 0 };
        public static IPAddress[] GetIPV6()
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                 .Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                 .Where(c => c.GetAddressBytes().AsSpan(0, 8).SequenceEqual(ipv6LocalBytes) == false).Distinct().ToArray();
        }
        public static IPAddress[] GetIPV4()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Where(c => c.IsIPv4MappedToIPv6 == false)
                .Where(c => c.Equals(IPAddress.Loopback) == false)
                .Distinct().ToArray();
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
