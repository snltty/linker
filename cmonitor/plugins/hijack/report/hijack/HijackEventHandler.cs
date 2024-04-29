using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using common.libs.winapis;
using System.Text;
using System.Buffers.Binary;
using common.libs;
using common.libs.extends;

namespace cmonitor.plugins.hijack.report.hijack
{
    public sealed class HijackEventHandler : NF_EventHandler
    {
        private readonly uint currentProcessId = 0;
        private readonly ConcurrentDictionary<ulong, ConnectionInfo> udpConnections = new ConcurrentDictionary<ulong, ConnectionInfo>();
        private readonly ConcurrentDictionary<ulong, ConnectionInfo> tcpConnections = new ConcurrentDictionary<ulong, ConnectionInfo>();

        private string[] processWhite = Array.Empty<string>();
        private string[] processBlack = Array.Empty<string>();
        private string[] domainWhite = Array.Empty<string>();
        private string[] domainBlack = Array.Empty<string>();
        private bool domainKill = false;
        private readonly ConcurrentDictionary<IPAddress, AllowType> domainIPs = new ConcurrentDictionary<IPAddress, AllowType>(new IPAddressComparer());
        private readonly ConcurrentQueue<DnsInfo> dnsDataQueue = new ConcurrentQueue<DnsInfo>();
        private uint updateLength = 0;

        public ulong UdpSend { get; private set; }
        public ulong UdpReceive { get; private set; }
        public ulong TcpSend { get; private set; }
        public ulong TcpReceive { get; private set; }

        public HijackEventHandler()
        {
            currentProcessId = (uint)Process.GetCurrentProcess().Id;
            UpdateDomainTask();
            DnsTask();
        }

        #region tcp无需处理
        public void tcpCanReceive(ulong id)
        {
        }
        public void tcpCanSend(ulong id)
        {
        }
        public unsafe void tcpConnectRequest(ulong id, ref NF_TCP_CONN_INFO pConnInfo)
        {
        }
        #endregion
        public unsafe void tcpConnected(ulong id, ref NF_TCP_CONN_INFO pConnInfo)
        {
            if (DeniedProcess(pConnInfo.processId, out string processName))
            {
                NFAPI.nf_tcpClose(id);
                return;
            }

            IPAddress ip;
            ushort port = 0;
            fixed (void* p = pConnInfo.remoteAddress)
            {
                byte* pp = (byte*)p;
                port = (ushort)(*(pp + 2) << 8 & 0xFF00 | *(pp + 3));
                if (DeniedIP(pConnInfo.processId, new nint(p), out ip) && port != 53)
                {
                    NFAPI.nf_tcpClose(id);
                    return;
                }
            }
            tcpConnections.TryAdd(id, new ConnectionInfo { RemoteIp = ip, RemotePort = port, Id = id, ProcessId = pConnInfo.processId });
            return;
        }
        public void tcpSend(ulong id, nint buf, int len)
        {
            if (tcpConnections.TryGetValue(id, out ConnectionInfo connection) == false)
            {
                NFAPI.nf_tcpClose(id);
                return;
            }
            if (connection.RemotePort != 53 && DeniedIP(connection.ProcessId, connection.RemoteIp))
            {
                NFAPI.nf_tcpClose(id);
                return;
            }
            TcpSend += (ulong)len;
            NFAPI.nf_tcpPostSend(id, buf, len);
        }
        public void tcpReceive(ulong id, nint buf, int len)
        {
            TcpReceive += (ulong)len;
            if (tcpConnections.TryGetValue(id, out ConnectionInfo connection) && connection.RemotePort == 53)
            {
                connection.PushDnsData(buf, len);
            }
            NFAPI.nf_tcpPostReceive(id, buf, len);
        }
        public void tcpClosed(ulong id, NF_TCP_CONN_INFO pConnInfo)
        {
            if (tcpConnections.TryRemove(id, out ConnectionInfo connection))
            {
                if (connection.RemotePort == 53)
                {
                    dnsDataQueue.Enqueue(new DnsInfo(connection.DnsData, connection.DnsLength, DnsProtocolType.TCP));
                    connection.ClearDnsData();
                }
            }
        }

        #region udp无需处理
        public void udpCanReceive(ulong id)
        {
        }
        public void udpCanSend(ulong id)
        {
        }
        public unsafe void udpConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST pConnReq)
        {
        }
        public void threadEnd()
        {
        }
        public void threadStart()
        {
        }

        #endregion
        public unsafe void udpReceive(ulong id, nint remoteAddress, nint buf, int len, nint options, int optionsLen)
        {
            byte* p = (byte*)remoteAddress;
            ushort port = (ushort)(*(p + 2) << 8 & 0xFF00 | *(p + 3));
            if (port == 53)
            {
                if (udpConnections.TryGetValue(id, out ConnectionInfo connection))
                {
                    connection.PushDnsData(buf, len);
                    dnsDataQueue.Enqueue(new DnsInfo(connection.DnsData, connection.DnsLength, DnsProtocolType.UDP));
                    connection.ClearDnsData();
                }
            }

            UdpReceive += (ulong)len;
            NFAPI.nf_udpPostReceive(id, remoteAddress, buf, len, options);
        }
        public void udpClosed(ulong id, NF_UDP_CONN_INFO pConnInfo)
        {
            udpConnections.TryRemove(id, out _);
        }
        public void udpCreated(ulong id, NF_UDP_CONN_INFO pConnInfo)
        {
            // 是阻止进程
            if (DeniedProcess(pConnInfo.processId, out string processName))
            {
                return;
            }
            udpConnections.TryAdd(id, new ConnectionInfo { Id = id, ProcessId = pConnInfo.processId });
        }
        public unsafe void udpSend(ulong id, nint remoteAddress, nint buf, int len, nint options, int optionsLen)
        {
            //地址数据指针
            byte* p = (byte*)remoteAddress;
            //端口,大端,需要翻转一下
            ushort port = (ushort)(*(p + 2) << 8 & 0xFF00 | *(p + 3));
            if (port != 53)
            {
                //丢包
                if (udpConnections.TryGetValue(id, out ConnectionInfo connection) == false || DeniedIP(connection.ProcessId, remoteAddress, out _))
                {
                    return;
                }
            }


            UdpSend += (ulong)len;
            NFAPI.nf_udpPostSend(id, remoteAddress, buf, len, options);
        }


        private bool processWhiteAny = false;
        private bool processBlackAny = false;
        /// <summary>
        /// 设置进程列表
        /// </summary>
        /// <param name="white"></param>
        /// <param name="black"></param>
        public void SetProcess(string[] white, string[] black)
        {
            processWhite = white;
            processWhiteAny = white.Any(c => c == "*");

            processBlack = black;
            processBlackAny = black.Any(c => c == "*");
        }
        /// <summary>
        /// 设置域名列表
        /// </summary>
        /// <param name="white"></param>
        /// <param name="black"></param>
        public void SetDomain(string[] white, string[] black, bool kill)
        {
            domainWhite = white;
            domainBlack = black;
            domainKill = kill;
            UpdateDomainFlag();
        }
        private void UpdateDomainTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    while (updateLength > 0)
                    {
                        await UpdateDomainIPs();
                        updateLength--;

                    }
                    await Task.Delay(3000);
                }
            });
            UpdateDomainFlag();
        }
        private async Task UpdateDomainIPs()
        {
            domainIPs.Clear();
            foreach (string domain in domainWhite)
            {
                if (domain == "*")
                {
                    domainIPs[IPAddress.Any] = AllowType.Allow;
                    continue;
                }
                try
                {
                    IPHostEntry entry = await Dns.GetHostEntryAsync(domain);
                    foreach (var item in entry.AddressList)
                    {
                        domainIPs[item] = AllowType.Allow;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(domain);
                    Logger.Instance.Error(ex);
                }
            }
            foreach (string domain in domainBlack)
            {
                if (domain == "*")
                {
                    domainIPs[IPAddress.Any] = AllowType.Denied;
                    continue;
                }

                try
                {
                    IPHostEntry entry = await Dns.GetHostEntryAsync(domain);
                    foreach (IPAddress item in entry.AddressList)
                    {
                        if (domainIPs.ContainsKey(item) == false)
                            domainIPs[item] = AllowType.Denied;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(domain);
                    Logger.Instance.Error(ex);
                }
            }
        }
        private void UpdateDomainFlag()
        {
            updateLength++;
        }
        /// <summary>
        /// dns解析
        /// </summary>
        private void DnsTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    while (dnsDataQueue.TryDequeue(out DnsInfo dns))
                    {
                        try
                        {
                            DnsUnpackResultInfo dnsPack = DnsUnpack(dns);
                            if (dnsPack == null)
                            {
                                continue;
                            }
                           
                            if (CheckName(domainWhite, dnsPack.Domain))
                            {
                                for (int i = 0; i < dnsPack.Ips.Length; i++)
                                {
                                    if (dnsPack.Ips[i] != null)
                                        domainIPs[dnsPack.Ips[i]] = AllowType.Allow;
                                }
                            }
                            else if (CheckName(domainBlack, dnsPack.Domain))
                            {
                                
                                for (int i = 0; i < dnsPack.Ips.Length; i++)
                                {
                                    if (dnsPack.Ips[i] != null)
                                    {
                                        domainIPs[dnsPack.Ips[i]] = AllowType.Denied;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error(ex);
                        }
                        finally
                        {
                            if (dns.Data != nint.Zero)
                            {
                                Marshal.FreeHGlobal(dns.Data);
                                dns.Data = nint.Zero;
                            }
                        }
                    }
                    await Task.Delay(15);
                }
            });
        }
        Memory<byte> domainCache = new byte[512];
        private unsafe DnsUnpackResultInfo DnsUnpack(DnsInfo dns)
        {
            Span<byte> span = new Span<byte>((void*)dns.Data, dns.Len);
            int index = 0;

            //tcp协议，头两个字节表示数据长度
            if (dns.ProtocolType == DnsProtocolType.TCP)
            {
                index += 2;
            }
            index += 2;//跳过transID
            ushort flag = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index));
            index += 2;
            ushort quesions = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index));
            index += 2;
            ushort answers = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index));
            index += 2;
            index += 4; //跳过 au 2字节 + ad 2字节

            byte rcode = (byte)(flag & 0b1111);
            byte tc = (byte)(flag >> 9 & 0x01);
            byte qr = (byte)(flag >> 15 & 0x01);
            if (rcode != 0 || tc != 0 || qr != 1 || quesions > 1)
            {
                return null; //错误，截断，非响应，大于1个查询
            }

            int domainPosition = 0;
            byte length = span[index];
            while (length > 0)
            {
                index += 1;//跳过长度字节

                span.Slice(index, length).CopyTo(domainCache.Span.Slice(domainPosition, length));
                domainPosition += length;
                domainCache.Span[domainPosition] = 46;//加个点.
                domainPosition += 1;

                index += length;
                length = span[index];
            }
            index += 1;//跳过长度字节
            domainPosition--; //去掉最后的点.

            ushort type = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index));
            index += 2; //type 
            if (type != 1 && type != 28)
            {
                return null; //不是A查询
            }
            index += 2; //跳过class 

            string domain = Encoding.UTF8.GetString(domainCache.Span.Slice(0, domainPosition));
            IPAddress[] ips = new IPAddress[answers];
            //answers
            for (int i = 0; i < answers; i++)
            {
                index += 2; //跳过指针
                type = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index));
                index += 2;
                index += 2;//跳过class
                index += 4;//跳过timeLive
                int dataLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index));
                index += 2;

                if (type == 1 || type == 28) //是A回应，其它的不要
                {
                    ips[i] = new IPAddress(span.Slice(index, dataLength));
                }
                index += dataLength;
            }
            return new DnsUnpackResultInfo { Domain = domain, Ips = ips };
        }
       

        /// <summary>
        /// 阻止ip
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <returns></returns>
        private unsafe bool DeniedIP(uint processId, nint remoteAddress, out IPAddress ip)
        {
            ip = ReadIPAddress(remoteAddress);
            return DeniedIP(processId, ip);
        }
        private unsafe bool DeniedIP(uint processId, IPAddress ip)
        {
            if (currentProcessId == processId)
            {
                return false;
            }
            try
            {
                bool res = domainIPs.TryGetValue(IPAddress.Any, out AllowType type) && type == AllowType.Denied;

                if (ip != null && domainIPs.TryGetValue(ip, out type))
                {
                    if (type == AllowType.Denied && domainKill)
                    {
                        try
                        {
                            string processName = NFAPI.nf_getProcessName(processId);
                            int index = processName.LastIndexOf('\\');
                            processName = Path.GetFileNameWithoutExtension(processName.Substring(index + 1, processName.Length - index - 1));
                            foreach (var item in Process.GetProcessesByName(processName))
                            {
                                item.Kill();
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    return type == AllowType.Denied;
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return false;
        }
        private unsafe IPAddress ReadIPAddress(nint remoteAddress)
        {
            //地址数据指针
            byte* p = (byte*)remoteAddress;
            //端口,大端,需要翻转一下
            ushort port = (ushort)(*(p + 2) << 8 & 0xFF00 | *(p + 3));
            //ip
            IPAddress ip = null;
            AddressFamily addressFamily = (AddressFamily)Marshal.ReadByte(remoteAddress);
            if (addressFamily == AddressFamily.InterNetwork)
            {
                ip = new IPAddress(new Span<byte>(p + 4, 4));
            }
            else if (addressFamily == AddressFamily.InterNetworkV6)
            {
                ip = new IPAddress(new Span<byte>(p + 8, 16));
            }
            return ip;
        }

        /// <summary>
        /// 是否阻止进程
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="processName"></param>
        /// <returns></returns>
        private bool DeniedProcess(uint processId, out string processName)
        {
            processName = string.Empty;
            if (currentProcessId == processId)
            {
                return false;
            }

            bool res = processBlackAny;

            processName = NFAPI.nf_getProcessName(processId);
            //白名单
            if (processWhite.Length > 0 && CheckName(processWhite, processName))
            {
                return false;
            }
            //黑名单
            if (processBlack.Length > 0 && CheckName(processBlack, processName))
            {
                return true;
            }

            return res;
        }


        private bool CheckName(string[] names, string path)
        {
            bool res = false;

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Length > path.Length) continue;

                var pathSpan = path.AsSpan();
                var nameSpan = names[i].AsSpan();
                try
                {
                    if (pathSpan.Slice(pathSpan.Length - nameSpan.Length, nameSpan.Length).SequenceEqual(nameSpan))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }
            return res;
        }


        sealed class IPAddressComparer : IEqualityComparer<IPAddress>
        {
            public bool Equals(IPAddress x, IPAddress y)
            {
                return x != null && y != null && x.Equals(y);
            }
            public int GetHashCode(IPAddress obj)
            {
                return obj.GetHashCode();
            }
        }
        sealed class ConnectionInfo
        {
            public ulong Id { get; set; }
            public uint ProcessId { get; set; }
            public IPAddress RemoteIp { get; set; }
            public ushort RemotePort { get; set; }

            public nint DnsData { get; set; }

            public int DnsLength { get; set; }

            public void PushDnsData(nint buf, int length)
            {
                //新容量
                int totalLength = DnsLength + length;
                nint distPtr = Marshal.AllocHGlobal(totalLength);

                //复制旧数据
                if (DnsData != nint.Zero)
                {
                    MSvcrt.memcpy(distPtr, DnsData, DnsLength);
                }
                //复制新数据
                MSvcrt.memcpy(distPtr + DnsLength, buf, length);

                if (DnsData != nint.Zero)
                {
                    Marshal.FreeHGlobal(DnsData);
                }

                DnsData = distPtr;
                DnsLength = totalLength;
            }
            public void ClearDnsData()
            {
                DnsData = IntPtr.Zero;
                DnsLength = 0;
            }
        }
        sealed class DnsUnpackResultInfo
        {
            public string Domain { get; set; }
            public IPAddress[] Ips { get; set; }
        }
        struct DnsInfo
        {
            public DnsInfo(nint data, int len, DnsProtocolType protocolType)
            {
                Data = data;
                Len = len;
                ProtocolType = protocolType;
            }
            public nint Data;
            public int Len;
            public DnsProtocolType ProtocolType;
        }
        enum DnsProtocolType : byte
        {
            UDP = 0,
            TCP = 1
        }

        enum AllowType : byte
        {
            Allow = 0,
            Denied = 1
        }
    }
}
