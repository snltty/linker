using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;

namespace LinkerRoute
{
    public class LinkerRoute
    {
        public string[] GetIPV4(string server)
        {
            List<string> result = new List<string>();
            var ips1 = GetRouteLevel(server);
            var ips2 = GetIPV4();

            foreach (var ip in ips1)
            {
                if (result.Contains(ip) == false)
                {
                    result.Add(ip);
                }
            }
            foreach (var ip in ips2)
            {
                if (result.Contains(ip) == false)
                {
                    result.Add(ip);
                }
            }
            return result.ToArray();
        }


        private string[] starts = new string[] { "10.", "100.", "192.168.", "172.", "127." };
        private string[] GetRouteLevel(string server)
        {
            if (string.IsNullOrWhiteSpace(server) == false)
            {
                server = server.Split(':')[0];
            }
            var ips = GetRouteLevelWindows(server);

            string[] result = new string[ips.Length];
            for (int i = 0; i < ips.Length; i++)
            {
                result[i] = ips[i].ToString();
            }
            return result;
        }
        private IPAddress[] GetRouteLevelWindows(string server)
        {
            List<IPAddress> result = new List<IPAddress>();
            try
            {
                IPAddress target = Dns.GetHostEntry(server).AddressList[0];

                for (ushort i = 1; i <= 5; i++)
                {
                    using (Ping pinger = new Ping())
                    {
                        PingReply reply = pinger.Send(target, 100, Encoding.ASCII.GetBytes("snltty"), new PingOptions { Ttl = i, DontFragment = true });


                        bool any = false;
                        string ip = reply.Address.ToString();
                        for (int k = 0; k < starts.Length; k++)
                        {
                            if (ip.IndexOf(starts[k]) == 0)
                            {
                                any = true;
                                break;
                            }
                        }

                        if (any)
                        {
                            result.Add(reply.Address);
                        }
                        else
                        {
                            return result.ToArray();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return result.ToArray();
        }

        public bool GetIsSameNetwork(string sourceIP, string distIP, byte prefixLength)
        {
            byte[] sourceBytes = IPAddress.Parse(sourceIP).GetAddressBytes();
            Array.Reverse(sourceBytes);

            byte[] distBytes = IPAddress.Parse(distIP).GetAddressBytes();
            Array.Reverse(distBytes);

            uint sourceIPInt = BitConverter.ToUInt32(sourceBytes, 0);
            uint distIPInt = BitConverter.ToUInt32(distBytes, 0);

            uint prefixIP = GetPrefixIP(prefixLength);

            return (sourceIPInt & prefixIP) == (distIPInt & prefixIP);
        }
        private uint GetPrefixIP(byte prefixLength)
        {
            //最多<<31 所以0需要单独计算
            if (prefixLength < 1) return 0;
            return 0xffffffff << (32 - prefixLength);
        }

        private string[] GetIPV4()
        {
            try
            {
                List<string> result = new List<string>();
                foreach (var item in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && item.IsIPv4MappedToIPv6 == false && item.Equals(IPAddress.Any) == false)
                    {
                        if (result.Contains(item.ToString()) == false)
                        {
                            result.Add(item.ToString());
                        }
                    }
                }
                return result.ToArray();
            }
            catch (Exception)
            {
            }
            return Array.Empty<string>();
        }
    }
}
