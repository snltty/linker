using linker.libs;
using linker.libs.timer;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
namespace linker.nat
{
    /// <summary>
    /// 开启后默认阻止
    /// </summary>
    public sealed class LinkerFirewall
    {
        private readonly ConcurrentDictionary<(string srcId, uint dst, ushort dstPort, ProtocolType pro), bool> cacheDstMap = new();
        private readonly ConcurrentDictionary<(uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro), SrcCacheInfo> cacheSrcMap = new();

        private List<LinkerFirewallRuleBuildInfo> buildedRules = [];
        private LinkerFirewallState state = LinkerFirewallState.Disabled;

        public VersionManager Version { get; private set; } = new VersionManager();

        public LinkerFirewall()
        {
            ClearTask();
        }

        public bool VersionChanged(ref ulong version)
        {
            bool result = Version.Eq(version, out ulong _version);
            version = _version;
            return result;
        }

        /// <summary>
        /// 是否启用防火墙
        /// </summary>
        /// <param name="state"></param>
        public void SetState(LinkerFirewallState state)
        {
            this.state = state;
            Version.Increment();
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
            cacheSrcMap.Clear();
            Version.Increment();
        }

        /// <summary>
        /// 添加一个允许，比如 192.168.1.1:12345->192.168.100.100:80，等下192.168.100.100:80->192.168.1.1:12345时要允许，越过防火墙
        /// </summary>
        /// <param name="packet">一个TCP/IP包</param>
        public unsafe void AddAllow(ReadOnlyMemory<byte> packet)
        {
            if (state != LinkerFirewallState.Enabled) return;

            fixed (byte* ptr = packet.Span)
            {
                FirewallPacket ipv4 = new FirewallPacket(ptr);
                if (ipv4.Version == 4 && (ipv4.Protocol == ProtocolType.Udp || ipv4.Protocol == ProtocolType.Tcp))
                {
                    (uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro) key = (ipv4.SrcAddr, ipv4.SrcPort, ipv4.DstAddr, ipv4.DstPort, ipv4.Protocol);
                    if (cacheSrcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        cache = new SrcCacheInfo { Type = SrcCacheType.Out };
                        cacheSrcMap.TryAdd(key, cache);
                    }
                    cache.LastTime = Environment.TickCount64;
                }
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
        public bool Check(string srcId, (uint ip, ushort port) dstEP, ProtocolType protocol)
        {
            if (this.state != LinkerFirewallState.Enabled) return true;
            return Check(srcId, dstEP.ip, dstEP.port, protocol);
        }


        /// <summary>
        /// 检查数据包是否符合规则
        /// </summary>
        /// <param name="srcId">客户端Id</param>
        /// <param name="packet">TCP/IP数据包</param>
        /// <returns></returns>
        public unsafe bool Check(string srcId, ReadOnlyMemory<byte> packet)
        {
            if (state != LinkerFirewallState.Enabled) return true;

            fixed (byte* ptr = packet.Span)
            {
                FirewallPacket ipv4 = new FirewallPacket(ptr);
                //IPV4 TCP 和 UDP
                if (ipv4.Version == 4 && (ipv4.Protocol == ProtocolType.Udp || ipv4.Protocol == ProtocolType.Tcp))
                {
                    //连接状态
                    (uint src, ushort srcPort, uint dst, ushort dstPort, ProtocolType pro) key = (ipv4.DstAddr, ipv4.DstPort, ipv4.SrcAddr, ipv4.SrcPort, ipv4.Protocol);
                    if (cacheSrcMap.TryGetValue(key, out SrcCacheInfo cache) == false)
                    {
                        cache = new SrcCacheInfo { Type = SrcCacheType.In };
                        cacheSrcMap.TryAdd(key, cache);
                    }
                    cache.LastTime = Environment.TickCount64;

                    //有出站标记 或 通过检查
                    return cache.Type == SrcCacheType.Out
                        || Check(srcId, ipv4.DstAddr, ipv4.DstPort, ipv4.Protocol);
                }

                return true;
            }
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
                bool match = (item.SrcIds == null ? (item.SrcId == "*" || item.SrcId == srcId) : item.SrcIds.Contains("*") || item.SrcIds.Contains(srcId))
                    && ((ip & item.DstPrefixLength) == item.DstNetwork)
                    && ((port >= item.DstPortStart && port <= item.DstPortEnd) || item.DstPorts.Contains(port))
                    && (item.Protocol & _rotocol) == _rotocol;
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
        sealed class SrcCacheInfo
        {
            public long LastTime { get; set; } = Environment.TickCount64;
            public SrcCacheType Type { get; set; }
        }
        enum SrcCacheType
        {
            In = 0,
            Out = 1
        }

        readonly unsafe struct FirewallPacket
        {
            private readonly byte* ptr;

            /// <summary>
            /// 协议版本
            /// </summary>
            public readonly byte Version => (byte)((*ptr >> 4) & 0b1111);
            public readonly ProtocolType Protocol => (ProtocolType)(*(ptr + 9));

            /// <summary>
            /// IP头长度
            /// </summary>
            public readonly int IPHeadLength => (*ptr & 0b1111) * 4;
            /// <summary>
            /// IP包荷载数据指针，也就是TCP/UDP头指针
            /// </summary>
            public readonly byte* PayloadPtr => ptr + IPHeadLength;

            /// <summary>
            /// 源地址
            /// </summary>
            public readonly uint SrcAddr
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 12));
                }
                set
                {
                    *(uint*)(ptr + 12) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            /// <summary>
            /// 源端口
            /// </summary>
            public readonly ushort SrcPort
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr));
                }
                set
                {
                    *(ushort*)(PayloadPtr) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            /// <summary>
            /// 目的地址
            /// </summary>
            public readonly uint DstAddr
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + 16));
                }
                set
                {
                    *(uint*)(ptr + 16) = BinaryPrimitives.ReverseEndianness(value);
                }
            }
            /// <summary>
            /// 目标端口
            /// </summary>
            public readonly ushort DstPort
            {
                get
                {
                    return BinaryPrimitives.ReverseEndianness(*(ushort*)(PayloadPtr + 2));
                }
                set
                {
                    *(ushort*)(PayloadPtr + 2) = BinaryPrimitives.ReverseEndianness(value);
                }
            }

            /// <summary>
            /// 加载TCP/IP包，必须是一个完整的TCP/IP包
            /// </summary>
            /// <param name="ptr">一个完整的TCP/IP包</param>
            public FirewallPacket(byte* ptr)
            {
                this.ptr = ptr;
            }
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
    [Flags]
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
