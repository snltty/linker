using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace common.libs.winapis
{
    public static class Wininet
    {
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int description, int reservedValue);


        [StructLayout(LayoutKind.Sequential)]
        public struct IP_ADAPTER_INFO
        {
            public IntPtr Next;
            public int ComboIndex;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string AdapterName;
            public int Index;
        }

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
        public struct MIB_IPNETROW2
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Address;
            public uint InterfaceIndex;
        }

        [DllImport("Iphlpapi.dll", SetLastError = true)]
        public static extern int DeleteIpNetEntry2(ref MIB_IPNETROW2 pArpEntry);
        public static void DeleteConnection(List<int> adapters,List<IPAddress> ips)
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

    }
}
