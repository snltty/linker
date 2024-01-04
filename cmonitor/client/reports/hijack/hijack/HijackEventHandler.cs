using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text;
using common.libs.extends;
using System.IO;
using cmonitor.client.reports.hijack;

namespace cmonitor.client.reports.hijack.hijack
{
    public sealed class HijackEventHandler : NF_EventHandler
    {
        private readonly HijackConfig hijackConfig;
        private readonly uint currentProcessId = 0;
        private readonly ConcurrentDictionary<ulong, bool> udpConnections = new ConcurrentDictionary<ulong, bool>();
        private readonly ConcurrentDictionary<ulong, bool> tcpConnections = new ConcurrentDictionary<ulong, bool>();

        public ulong UdpSend { get; private set; }
        public ulong UdpReceive { get; private set; }
        public ulong TcpSend { get; private set; }
        public ulong TcpReceive { get; private set; }

        public HijackEventHandler(HijackConfig hijackConfig)
        {
            this.hijackConfig = hijackConfig;
            currentProcessId = (uint)Process.GetCurrentProcess().Id;
        }

        #region tcp无需处理
        public void tcpCanReceive(ulong id)
        {
        }
        public void tcpCanSend(ulong id)
        {
        }
        public void tcpConnectRequest(ulong id, ref NF_TCP_CONN_INFO pConnInfo)
        {

        }
        public void tcpConnected(ulong id, NF_TCP_CONN_INFO pConnInfo)
        {
            //是阻止进程
            if (checkProcess(pConnInfo.processId, out string processName))
            {
                return;
            }
            tcpConnections.TryAdd(id, true);
            return;
        }
        public void tcpSend(ulong id, nint buf, int len)
        {
            if (tcpConnections.ContainsKey(id))
            {
                TcpSend += (ulong)len;
                NFAPI.nf_tcpPostSend(id, buf, len);
            }
        }
        public void tcpReceive(ulong id, nint buf, int len)
        {
            TcpReceive += (ulong)len;
            NFAPI.nf_tcpPostReceive(id, buf, len);
        }
        public void tcpClosed(ulong id, NF_TCP_CONN_INFO pConnInfo)
        {
            tcpConnections.TryRemove(id, out _);
        }
        #endregion

        #region udp无需处理
        public void udpCanReceive(ulong id)
        {
        }
        public void udpCanSend(ulong id)
        {
        }
        public void udpConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST pConnReq)
        {
        }
        public void threadEnd()
        {
        }
        public void threadStart()
        {
        }
        public void udpReceive(ulong id, nint remoteAddress, nint buf, int len, nint options, int optionsLen)
        {
            UdpReceive += (ulong)len;
            NFAPI.nf_udpPostReceive(id, remoteAddress, buf, len, options);
        }
        #endregion
        public void udpClosed(ulong id, NF_UDP_CONN_INFO pConnInfo)
        {
            udpConnections.TryRemove(id, out _);
            //删除udp对象缓存
        }
        public void udpCreated(ulong id, NF_UDP_CONN_INFO pConnInfo)
        {
            //是阻止进程
            if (checkProcess(pConnInfo.processId, out string processName))
            {
                return;
            }
            udpConnections.TryAdd(id, true);
        }
        public unsafe void udpSend(ulong id, nint remoteAddress, nint buf, int len, nint options, int optionsLen)
        {
            //丢弃进程包
            if (udpConnections.TryGetValue(id, out _) == false)
            {
                return;
            }
            //丢弃域名包
            if (checkDomain(remoteAddress, buf, len))
            {
                return;
            }

            UdpSend += (ulong)len;
            NFAPI.nf_udpPostSend(id, remoteAddress, buf, len, options);
        }

        /// <summary>
        /// 是否阻止域名
        /// </summary>
        /// <param name="remoteAddress"></param>
        /// <param name="buf"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private unsafe bool checkDomain(nint remoteAddress, nint buf, int len)
        {
            if (hijackConfig.AllowDomains.Length == 0 && hijackConfig.DeniedDomains.Length == 0)
            {
                return false;
            }

            byte* p = (byte*)remoteAddress;
            ushort port = (ushort)(*(p + 2) << 8 & 0xFF00 | *(p + 3));
            if (port == 53)
            {
                try
                {
                    Span<byte> span = new Span<byte>((void*)buf, len);
                    span = span.Slice(4);
                    ushort length = (ushort)(span[0] << 8 | span[1]);
                    span = span.Slice(8);

                    for (int i = 0; i < length; i++)
                    {
                        StringBuilder sb = new StringBuilder(256);
                        while (span[0] > 0)
                        {
                            sb.Append(span.Slice(1, span[0]).GetString());
                            sb.Append('.');
                            span = span.Slice(1 + span[0]);
                        }
                        string domain = sb.ToString(0, sb.Length - 1);
                        if (checkDomain(domain))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    return true;
                }
            }
            return false;
        }
        private bool checkDomain(string domain)
        {
            //黑名单
            if (hijackConfig.DeniedDomains.Length > 0 && checkName(hijackConfig.DeniedDomains, domain))
            {
                return true;
            }
            //白名单
            if (hijackConfig.AllowDomains.Length > 0)
            {
                return checkName(hijackConfig.AllowDomains, domain) == false;
            }
            return false;
        }

        /// <summary>
        /// 是否阻止进程
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="processName"></param>
        /// <returns></returns>
        private bool checkProcess(uint processId, out string processName)
        {
            processName = string.Empty;
            if (currentProcessId == processId)
            {
                return false;
            }
            processName = NFAPI.nf_getProcessName(processId);
            //黑名单
            if (hijackConfig.DeniedProcesss.Length > 0 && checkName(hijackConfig.DeniedProcesss, processName))
            {
                return true;
            }
            //白名单
            if (hijackConfig.AllowProcesss.Length > 0)
            {
                return checkName(hijackConfig.AllowProcesss, processName) == false;
            }

            return false;
        }


        private bool checkName(string[] names, string path)
        {
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
                catch (Exception ex)
                {
                    Debug.WriteLine(ex + "");
                }
            }
            return false;
        }

    }
}
