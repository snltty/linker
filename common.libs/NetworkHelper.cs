using System;
using System.Net;
using System.Net.Sockets;

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
                if (string.IsNullOrWhiteSpace(domain)) return null;
                if (IPAddress.TryParse(domain, out IPAddress ip))
                {
                    return ip;
                }
                IPAddress[] list = Dns.GetHostEntry(domain).AddressList;
                if (list.Length > 0) return list[0];
                return null;
            }
            catch (Exception)
            {
            }
            return null;
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
