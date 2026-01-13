using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace linker.libs.winapis
{
    public static class Wininet
    {
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int description, int reservedValue);



        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetAdaptersInfo(IntPtr pAdapterInfo, ref int pBufOutLen);
        public static List<int> GetAdaptersIndex()
        {
            List<int> list = new List<int>();
            int bufLen = 4096;
            IntPtr pAdapterInfo = Marshal.AllocHGlobal(bufLen);

            int result = GetAdaptersInfo(pAdapterInfo, ref bufLen);
            if (result == 0)
            {
                IntPtr pAdapter = pAdapterInfo;
                while (pAdapter != IntPtr.Zero)
                {
                    IP_ADAPTER_INFO adapterInfo = (IP_ADAPTER_INFO)Marshal.PtrToStructure(pAdapter, typeof(IP_ADAPTER_INFO));
                    list.Add(adapterInfo.ComboIndex);
                    pAdapter = adapterInfo.Next;
                }
            }

            Marshal.FreeHGlobal(pAdapterInfo);

            return list;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IP_ADAPTER_INFO
        {
            public IntPtr Next;
            public int ComboIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string AdapterName;
            public int Index;
        }



        [DllImport("Iphlpapi.dll", SetLastError = true)]
        public static extern int DeleteIpNetEntry2(ref MIB_IPNETROW2 pArpEntry);
        public static void DeleteConnection(List<int> adapters, List<IPAddress> ips)
        {
            try
            {
                foreach (var ip in ips)
                {
                    foreach (var adapter in adapters)
                    {
                        byte[] addressBytes = ip.GetAddressBytes();
                        MIB_IPNETROW2 row = new MIB_IPNETROW2
                        {
                            Address = addressBytes,
                            InterfaceIndex = (uint)adapter
                        };
                        int result = DeleteIpNetEntry2(ref row);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        public static void DeleteConnection1(List<IPAddress> ips)
        {
            try
            {
                Span<byte> bytes = stackalloc byte[4];

                foreach (var ip in ips)
                {
                    MIB_TCPROW tcpRow = new MIB_TCPROW();

                    ip.TryWriteBytes(bytes, out _);
                    tcpRow.dwRemoteAddr = BitConverter.ToUInt32(bytes);
                    tcpRow.dwRemotePort = (ushort)IPAddress.HostToNetworkOrder((short)443);
                    tcpRow.dwState = 12; // MIB_TCP_STATE_DELETE_TCB

                    int result = SetTcpEntry(ref tcpRow);
                }
            }
            catch (Exception)
            {
            }
        }
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int SetTcpEntry(ref MIB_TCPROW row);
        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_IPNETROW2
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Address;
            public uint InterfaceIndex;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
        }



        public static List<ConnectionInfo> GetTcpConnections()
        {
            List<ConnectionInfo> connections = new List<ConnectionInfo>();
            int buffSize = 0;
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            if (result == 0)
            {
                return connections;
            }

            IntPtr tcpTablePtr = Marshal.AllocHGlobal(buffSize);
            try
            {
                result = GetExtendedTcpTable(tcpTablePtr, ref buffSize, false, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (result != 0)
                {
                    return connections;
                }
                MIB_TCPTABLE_OWNER_PID tcpTable = (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(tcpTablePtr, typeof(MIB_TCPTABLE_OWNER_PID));

                IntPtr rowPtr = (nint)((long)tcpTablePtr + Marshal.SizeOf(tcpTable.dwNumEntries));
                for (int i = 0; i < tcpTable.dwNumEntries; i++)
                {
                    MIB_TCPROW_OWNER_PID row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    //转换字节序
                    ushort localPort = (ushort)row.localPort;
                    localPort = (ushort)(((localPort >> 8) & 0xffff) | ((localPort << 8) & 0xffff));
                    ushort remotePort = (ushort)row.remotePort;
                    remotePort = (ushort)(((remotePort >> 8) & 0xffff) | ((remotePort << 8) & 0xffff));

                    IPEndPoint localEndPoint = new IPEndPoint(row.localAddr, localPort);
                    IPEndPoint remoteEndPoint = new IPEndPoint(row.remoteAddr, remotePort);

                    connections.Add(new ConnectionInfo { LocalEndPoint = localEndPoint, RemoteEndPoint = remoteEndPoint, Pid = row.owningPid });

                    rowPtr = (nint)((long)rowPtr + Marshal.SizeOf(row));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
            return connections;

        }
        public static List<ConnectionInfo> GetUdpConnections()
        {
            List<ConnectionInfo> connections = new List<ConnectionInfo>();

            MIB_UDPTABLE_OWNER_PID udpTable;
            uint udpTableSize = 0;
            // 获取 UDP 表格大小
            uint value = GetExtendedUdpTable(IntPtr.Zero, ref udpTableSize, true, AF_INET, 5, 0);
            // 分配内存
            IntPtr udpTablePtr = Marshal.AllocHGlobal((int)udpTableSize);

            try
            {
                // 获取 UDP 表格信息
                if (GetExtendedUdpTable(udpTablePtr, ref udpTableSize, true, AF_INET, 5, 0) == 0)
                {
                    udpTable = (MIB_UDPTABLE_OWNER_PID)Marshal.PtrToStructure(udpTablePtr, typeof(MIB_UDPTABLE_OWNER_PID));

                    for (int i = 0; i < udpTable.dwNumEntries; i++)
                    {
                        UDPROW_OWNER_PID udpRow = (UDPROW_OWNER_PID)Marshal.PtrToStructure(new IntPtr(udpTablePtr.ToInt64() + Marshal.SizeOf(typeof(uint)) + i * Marshal.SizeOf(typeof(UDPROW_OWNER_PID))), typeof(UDPROW_OWNER_PID));

                        ushort localPort = (ushort)udpRow.dwLocalPort;
                        localPort = (ushort)(((localPort >> 8) & 0xffff) | ((localPort << 8) & 0xffff));
                        IPEndPoint localEndPoint = new IPEndPoint(udpRow.dwLocalAddr, localPort);
                        connections.Add(new ConnectionInfo { LocalEndPoint = localEndPoint, Pid = (int)udpRow.dwOwningPid });

                    }
                }
            }
            finally
            {
                // 释放内存
                Marshal.FreeHGlobal(udpTablePtr);
            }
            return connections;
        }
        public sealed class ConnectionInfo
        {
            public int Pid { get; set; }
            public IPEndPoint LocalEndPoint { get; set; }
            public IPEndPoint RemoteEndPoint { get; set; }
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, int reserved);
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref uint pdwSize, bool bOrder, int ulAf, int TableClass, uint Reserved);
        private const int AF_INET = 2;  // IPv4
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER = 0,
            TCP_TABLE_BASIC_CONNECTIONS = 1,
            TCP_TABLE_BASIC_ALL = 2,
            TCP_TABLE_OWNER_PID_LISTENER = 3,
            TCP_TABLE_OWNER_PID_CONNECTIONS = 4,
            TCP_TABLE_OWNER_PID_ALL = 5,
            TCP_TABLE_OWNER_MODULE_LISTENER = 6,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS = 7,
            TCP_TABLE_OWNER_MODULE_ALL = 8
        }
        [StructLayout(LayoutKind.Sequential)]
        struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public int owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
        }

        // 定义 MIB_UDPTABLE_OWNER_PID 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            public UDPROW_OWNER_PID[] table;
        }

        // 定义 UDPROW_OWNER_PID 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct UDPROW_OWNER_PID
        {
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwOwningPid;
        }

    }
}
