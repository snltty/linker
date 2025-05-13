using linker.libs;
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
        private ConcurrentDictionary<(string srcId, uint dst, ushort dstPort, ProtocolType pro), bool> cacheMap = new ConcurrentDictionary<(string srcId, uint dst, ushort dstPort, ProtocolType pro), bool>();

        private List<LinkerFirewallRuleBuildInfo> buildedRules = new List<LinkerFirewallRuleBuildInfo>();
        private LinkerFirewallState state = LinkerFirewallState.Disabled;

        public LinkerFirewall()
        {
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

            cacheMap.Clear();
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

            uint dst = ipv4.DstAddr;
            ushort dstPort = ipv4.Protocol switch
            {
                ProtocolType.Udp => ipv4.DstPort,
                ProtocolType.Tcp => ipv4.DstPort,
                _ => 0,
            };
            return Check(srcId, dst, dstPort, ipv4.Protocol);
        }

        private bool Check(string srcId, uint ip, ushort port, ProtocolType protocol)
        {
            LinkerFirewallProtocolType _rotocol = protocol switch
            {
                ProtocolType.Tcp => LinkerFirewallProtocolType.TCP,
                ProtocolType.Udp => LinkerFirewallProtocolType.UDP,
                _ => LinkerFirewallProtocolType.None,
            };
            if (_rotocol == LinkerFirewallProtocolType.None) return true; //不支持的协议

            //之前已经检查过
            (string srcId, uint dst, ushort dstPort, ProtocolType pro) key = (srcId, ip, port, protocol);
            if (cacheMap.TryGetValue(key, out bool value))
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
                    cacheMap.AddOrUpdate(key, value, (a, b) => value);
                    return value;
                }
            }
            return false;
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
}
