using linker.libs;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using static linker.snat.LinkerSrcNat;
namespace linker.snat
{
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

            //末尾有一条默认允许所有的规则
            rules.Add(new LinkerFirewallRuleInfo
            {
                SrcId = string.Empty,
                DstCIDR = "0.0.0.0/0",
                DstPort = "0",
                Protocol = LinkerFirewallProtocolType.All,
                Action = LinkerFirewallAction.Allow,
            });

            buildedRules = rules.Select(c =>
            {
                string[] ports = c.DstPort.Split('-');
                int portStart = int.Parse(ports[0]), portEnd = portStart;
                if (ports.Length == 2)
                {
                    portEnd = int.Parse(ports[1]);
                }
                else if (portStart == 0)
                {
                    portEnd = 65535;
                }

                string[] cidr = c.DstCIDR.Split('/');
                IPAddress ip = IPAddress.Parse(cidr[0]);
                byte prefixLength = 32;
                if (cidr.Length == 2)
                {
                    prefixLength = byte.Parse(cidr[1]);
                }

                return new LinkerFirewallRuleBuildInfo
                {
                    SrcId = c.SrcId,
                    DstNetwork = NetworkHelper.ToNetworkValue(ip, prefixLength),
                    DstPrefixLength = NetworkHelper.ToPrefixValue(prefixLength),
                    DstPortStart = portStart,
                    DstPortEnd = portEnd,
                    Protocol = c.Protocol,
                    Action = c.Action
                };
            }).ToList();

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
            uint dst = NetworkHelper.ToValue(dstEP.Address);
            ushort dstPort = (ushort)dstEP.Port;
            return Check(srcId, dst, dstPort, (byte)(dstEP.AddressFamily == AddressFamily.InterNetwork ? 4 : 6), protocol);
        }
        /// <summary>
        /// 检查数据包是否符合规则
        /// </summary>
        /// <param name="srcId">客户端Id</param>
        /// <param name="packet">TCP/IP数据包</param>
        /// <returns></returns>
        public bool Check(string srcId, ReadOnlyMemory<byte> packet)
        {
            IPV4Packet ipv4 = new IPV4Packet(packet.Span);
            uint dst = ipv4.DstAddr;
            ushort dstPort = ipv4.Protocol switch
            {
                ProtocolType.Udp => ipv4.DstPort,
                ProtocolType.Tcp => ipv4.DstPort,
                _ => 0,
            };
            return Check(srcId, dst, dstPort, ipv4.Version, ipv4.Protocol);
        }

        private bool Check(string srcId, uint ip, ushort port, byte version, ProtocolType protocol)
        {
            //防火墙未启用
            if (this.state != LinkerFirewallState.Enabled)
            {
                return true;
            }
            //没有配置规则
            if (buildedRules.Count == 0)
            {
                return true;
            }
            //仅IPV4
            if (version != 4)
            {
                return true;
            }

            LinkerFirewallProtocolType _rotocol = protocol switch
            {
                ProtocolType.Icmp => LinkerFirewallProtocolType.ICMP,
                ProtocolType.Tcp => LinkerFirewallProtocolType.TCP,
                ProtocolType.Udp => LinkerFirewallProtocolType.UDP,
                _ => LinkerFirewallProtocolType.None,
            };
            //不在协议列表内
            if (_rotocol == LinkerFirewallProtocolType.None)
            {
                return true;
            }

            //之前已经检查过
            (string srcId, uint dst, ushort dstPort, ProtocolType pro) key = (srcId, ip, port, protocol);
            if (cacheMap.TryGetValue(key, out bool value))
            {
                return value;
            }

            //按顺序匹配规则
            foreach (LinkerFirewallRuleBuildInfo item in buildedRules)
            {
                bool match = (string.IsNullOrWhiteSpace(item.SrcId) || item.SrcId == srcId)
                    && ((ip & item.DstPrefixLength) == item.DstNetwork)
                    && (port >= item.DstPortStart && port <= item.DstPortEnd)
                    && item.Protocol.HasFlag(_rotocol);
                if (match)
                {
                    value = item.Action == LinkerFirewallAction.Allow;
                    cacheMap.AddOrUpdate(key, value, (a, b) => value);
                    return value;
                }
            }
            return true;
        }


        sealed class LinkerFirewallRuleBuildInfo
        {
            public string SrcId { get; set; } = string.Empty;

            public uint DstNetwork { get; set; }
            public uint DstPrefixLength { get; set; }
            public int DstPortStart { get; set; }
            public int DstPortEnd { get; set; }

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
        ICMP = 4,

        All = TCP | UDP | ICMP
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
