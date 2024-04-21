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
                return Dns.GetHostEntry(domain).AddressList.FirstOrDefault();
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
                    if (ip.StartsWith(starts[0], StringComparison.Ordinal) || ip.StartsWith(starts[1], StringComparison.Ordinal) || ip.StartsWith(starts[2], StringComparison.Ordinal) || ip.StartsWith(starts[3], StringComparison.Ordinal))
                    {
                        if (ip.StartsWith(starts[2], StringComparison.Ordinal) == false)
                            ips.Add(list.ElementAt(i));
                    }
                    else
                    {
                        if (i == 0) return 1;
                        return i;
                    }
                }
            }
            catch (Exception)
            {
            }
            return 1;
        }
        public static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress)
        {
            return GetTraceRoute(hostNameOrAddress, 1);
        }
        private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl)
        {
            Ping pinger = new();
            // 创建PingOptions对象
            PingOptions pingerOptions = new(ttl, true);
            int timeout = 100;
            byte[] buffer = Encoding.ASCII.GetBytes("11");
            // 创建PingReply对象
            // 发送ping命令
            PingReply reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);

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
