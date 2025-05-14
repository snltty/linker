using linker.libs;
using linker.libs.timer;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using static linker.snat.LinkerSrcNat;
namespace linker.snat
{
    /// <summary>
    /// 开启后默认阻止
    /// </summary>
    public sealed class LinkerFirewall
    {
        private ConcurrentDictionary<(string srcId, uint dst, ushort dstPort, ProtocolType pro), bool> cacheDstMap = new ConcurrentDictionary<(string srcId, uint dst, ushort dstPort, ProtocolType pro), bool>();
        private ConcurrentDictionary<(uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro), SrcCacheInfo> cacheSrcMap = new ConcurrentDictionary<(uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro), SrcCacheInfo>();

        private List<LinkerFirewallRuleBuildInfo> buildedRules = new List<LinkerFirewallRuleBuildInfo>();
        private LinkerFirewallState state = LinkerFirewallState.Disabled;

        public LinkerFirewall()
        {
            ClearTask();
        }

        /// <summary>
        /// 是否启用防火墙
        /// </summary>
        /// <param name="state"></param>
        public void SetState(LinkerFirewallState state)
        {
            this.state = state;
        }

        /// <summary>
        /// 构建规则
        /// </summary>
        /// <param name="rules"></param>
        public void BuildRules(List<LinkerFirewallRuleInfo> rules)
        {
            if (rules.Count == 0)
            {
                buildedRules = new List<LinkerFirewallRuleBuildInfo>();
                return;
            }
            buildedRules = rules.Select(c =>
            {
                try
                {
                    int portStart = 0, portEnd = 65535;
                    HashSet<int> ports = new HashSet<int>();
                    if (c.DstPort != "0" && c.DstPort != "*")
                    {
                        if (c.DstPort.Contains('-'))
                        {
                            string[] arr = c.DstPort.Split('-');
                            portStart = int.Parse(arr[0]);
                            portEnd = int.Parse(arr[1]);
                        }
                        else if (c.DstPort.Contains(','))
                        {
                            ports = c.DstPort.Split(',').Select(c => int.Parse(c)).ToHashSet();
                        }
                        else
                        {
                            portEnd = portStart = int.Parse(c.DstPort);
                        }
                    }

                    IPAddress ip = IPAddress.Any;
                    byte prefixLength = 0;
                    if (c.DstCIDR != "0" && c.DstCIDR != "*")
                    {
                        string[] cidr = c.DstCIDR.Split('/');
                        ip = IPAddress.Parse(cidr[0]);
                        prefixLength = 32;
                        if (cidr.Length == 2)
                        {
                            prefixLength = byte.Parse(cidr[1]);
                        }
                        if (prefixLength > 32) prefixLength = 32;
                    }


                    IEnumerable<string> ids = c.SrcId.Split(',').Distinct();

                    return new LinkerFirewallRuleBuildInfo
                    {
                        SrcIds = ids.Count() > 1 ? ids.ToHashSet() : null,
                        SrcId = ids.Count() <= 1 ? c.SrcId : string.Empty,
                        DstNetwork = NetworkHelper.ToNetworkValue(ip, prefixLength),
                        DstPrefixLength = NetworkHelper.ToPrefixValue(prefixLength),
                        DstPortStart = portStart,
                        DstPortEnd = portEnd,
                        DstPorts = ports,
                        Protocol = c.Protocol,
                        Action = c.Action
                    };
                }
                catch (Exception)
                {

                }
                return null;

            }).Where(c => c != null).ToList();

            cacheDstMap.Clear();
        }

        /// <summary>
        /// 添加一个允许，比如 192.168.1.1:12345->192.168.100.100:80，等下192.168.100.100:80->192.168.1.1:12345时要允许，越过防火墙
        /// </summary>
        /// <param name="packet">一个TCP/IP包</param>
        public void AddAllow(ReadOnlyMemory<byte> packet)
        {
            //未启用防火墙
            if (this.state != LinkerFirewallState.Enabled) return;

            IPV4Packet ipv4 = new IPV4Packet(packet.Span);
            //不是ipv4不管
            if (ipv4.Version != 4) return;

            //只需要处理TCP/UDP
            if (ipv4.Protocol == ProtocolType.Udp || ipv4.Protocol == ProtocolType.Tcp)
            {
                (uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro) key = (ipv4.SrcAddr, ipv4.SrcPort, ipv4.DstAddr, ipv4.DstPort, ipv4.Protocol);
                if (cacheSrcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                {
                    cache = new SrcCacheInfo();
                    cacheSrcMap.TryAdd(key, cache);
                }
                cache.LastTime = Environment.TickCount64;
            }
        }

        /// <summary>
        /// 检查数据包是否符合规则
        /// </summary>
        /// <param name="srcId">客户端Id</param>
        /// <param name="dstEP">目标服务</param>
        /// <param name="protocol">协议</param>
        /// <returns></returns>
        public bool Check(string srcId, IPEndPoint dstEP, ProtocolType protocol)
        {
            if (this.state != LinkerFirewallState.Enabled) return true;
            if (dstEP.AddressFamily != AddressFamily.InterNetwork) return false;

            uint dst = NetworkHelper.ToValue(dstEP.Address);
            ushort dstPort = (ushort)dstEP.Port;
            return Check(srcId, dst, dstPort, protocol);
        }
        /// <summary>
        /// 检查数据包是否符合规则
        /// </summary>
        /// <param name="srcId">客户端Id</param>
        /// <param name="packet">TCP/IP数据包</param>
        /// <returns></returns>
        public bool Check(string srcId, ReadOnlyMemory<byte> packet)
        {
            if (this.state != LinkerFirewallState.Enabled) return true;

            IPV4Packet ipv4 = new IPV4Packet(packet.Span);
            if (ipv4.Version != 4) return true;

            if (ipv4.Protocol == ProtocolType.Udp || ipv4.Protocol == ProtocolType.Tcp)
            {
                (uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro) key = (ipv4.DstAddr, ipv4.DstPort, ipv4.SrcAddr, ipv4.SrcPort, ipv4.Protocol);
                if (cacheSrcMap.TryGetValue(key, out SrcCacheInfo cache))
                {
                    cache.LastTime = Environment.TickCount64;
                    return true;
                }
                return Check(srcId, ipv4.DstAddr, ipv4.DstPort, ipv4.Protocol);
            }
            return true;
        }

        private bool Check(string srcId, uint ip, ushort port, ProtocolType protocol)
        {
            LinkerFirewallProtocolType _rotocol = protocol switch
            {
                ProtocolType.Tcp => LinkerFirewallProtocolType.TCP,
                ProtocolType.Udp => LinkerFirewallProtocolType.UDP,
                _ => LinkerFirewallProtocolType.None,
            };

            //之前已经检查过
            (string srcId, uint dst, ushort dstPort, ProtocolType pro) key = (srcId, ip, port, protocol);
            if (cacheDstMap.TryGetValue(key, out bool value))
            {
                return value;
            }

            //按顺序匹配规则
            foreach (LinkerFirewallRuleBuildInfo item in buildedRules)
            {
                bool match = (item.SrcIds == null ? item.SrcId == "*" || item.SrcId == srcId : item.SrcIds.Contains("*") || item.SrcIds.Contains(srcId))
                    && ((ip & item.DstPrefixLength) == item.DstNetwork)
                    && ((port >= item.DstPortStart && port <= item.DstPortEnd) || item.DstPorts.Contains(port))
                    && item.Protocol.HasFlag(_rotocol);
                if (match)
                {
                    value = item.Action == LinkerFirewallAction.Allow;
                    cacheDstMap.TryAdd(key, value);
                    return value;
                }
            }
            return false;
        }


        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                long now = Environment.TickCount64;
                foreach (var item in cacheSrcMap.Where(c => now - c.Value.LastTime > 60 * 60 * 2 * 1000).Select(c => c.Key).ToList())
                {
                    cacheSrcMap.TryRemove(item, out _);
                }
            }, 5000);
        }

        sealed class LinkerFirewallRuleBuildInfo
        {
            public string SrcId { get; set; }
            public HashSet<string> SrcIds { get; set; }

            public uint DstNetwork { get; set; }
            public uint DstPrefixLength { get; set; }
            public int DstPortStart { get; set; }
            public int DstPortEnd { get; set; }

            public HashSet<int> DstPorts { get; set; }

            public LinkerFirewallProtocolType Protocol { get; set; }
            public LinkerFirewallAction Action { get; set; }
        }
    }
    public class LinkerFirewallRuleInfo
    {
        public string SrcId { get; set; } = string.Empty;

        public string DstCIDR { get; set; } = "0.0.0.0/0";
        public string DstPort { get; set; } = "0";

        public LinkerFirewallProtocolType Protocol { get; set; }
        public LinkerFirewallAction Action { get; set; }
    }

    public enum LinkerFirewallProtocolType : byte
    {
        None = 0,
        TCP = 1,
        UDP = 2,
        All = TCP | UDP
    }
    public enum LinkerFirewallAction
    {
        Allow = 1,
        Deny = 2,
        All = Allow | Deny
    }
    public enum LinkerFirewallState
    {
        Enabled = 0,
        Disabled = 1
    }

    public sealed class SrcCacheInfo
    {
        public long LastTime { get; set; } = Environment.TickCount64;
    }
}
