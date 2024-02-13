using common.libs;
using common.libs.winapis;
using SharpDX;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace cmonitor.client.reports.volume
{
    public sealed class WlanReport : IReport
    {
        public string Name => "Wlan";


        Dictionary<string, Wlanapi.WLAN_AVAILABLE_NETWORK> wlans = new Dictionary<string, Wlanapi.WLAN_AVAILABLE_NETWORK>();

        private readonly Config config;
        private readonly ClientConfig clientConfig;
        public WlanReport(Config config, ClientConfig clientConfig)
        {
            this.config = config;
            this.clientConfig = clientConfig;
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        public void WlanEnums(nint wlanHandle)
        {
            IntPtr hClientHandle;
            uint negotiatedVersion;
            uint result = Wlanapi.WlanOpenHandle(2, IntPtr.Zero, out negotiatedVersion, out hClientHandle);
            if (result != 0)
            {
                return;
            }

            IntPtr pInterfaceList;
            result = Wlanapi.WlanEnumInterfaces(wlanHandle, IntPtr.Zero, out pInterfaceList);
            if (result != 0)
            {
                Wlanapi.WlanCloseHandle(wlanHandle, IntPtr.Zero);
                return;
            }
            Wlanapi.WLAN_INTERFACE_INFO_LIST interfaceList = (Wlanapi.WLAN_INTERFACE_INFO_LIST)Marshal.PtrToStructure(pInterfaceList, typeof(Wlanapi.WLAN_INTERFACE_INFO_LIST));

            if (interfaceList.dwNumberOfItems > 0)
            {
                Guid interfaceGuid = interfaceList.InterfaceInfo[0].InterfaceGuid;

                IntPtr pAvailableNetworkList;
                result = Wlanapi.WlanGetAvailableNetworkList(wlanHandle, ref interfaceGuid, 0, IntPtr.Zero, out pAvailableNetworkList);
                if (result != 0)
                {
                    Wlanapi.WlanFreeMemory(pInterfaceList);
                    Wlanapi.WlanCloseHandle(wlanHandle, IntPtr.Zero);
                    return;
                }

                Wlanapi.WLAN_AVAILABLE_NETWORK_LIST availableNetworkList = (Wlanapi.WLAN_AVAILABLE_NETWORK_LIST)Marshal.PtrToStructure(pAvailableNetworkList, typeof(Wlanapi.WLAN_AVAILABLE_NETWORK_LIST));
                foreach (Wlanapi.WLAN_AVAILABLE_NETWORK network in availableNetworkList.Network)
                {
                    // 这里可以根据需要修改要连接的网络名称和密码
                    if (network.dot11Ssid.uSSIDLength == 6)
                    {
                        wlans[Encoding.ASCII.GetString(network.dot11Ssid.ucSSID, 0, (int)network.dot11Ssid.uSSIDLength)] = network;
                    }
                }

                Wlanapi.WlanFreeMemory(pAvailableNetworkList);
            }

            Wlanapi.WlanFreeMemory(pInterfaceList);
            Wlanapi.WlanCloseHandle(wlanHandle, IntPtr.Zero);
        }

        public void WlanConnect(string name)
        {
            IntPtr hClientHandle;
            uint negotiatedVersion;
            uint result = Wlanapi.WlanOpenHandle(2, IntPtr.Zero, out negotiatedVersion, out hClientHandle);
            if (result != 0)
            {
                return;
            }

            IntPtr pInterfaceList;
            result = Wlanapi.WlanEnumInterfaces(hClientHandle, IntPtr.Zero, out pInterfaceList);
            if (result != 0)
            {
                Wlanapi.WlanCloseHandle(hClientHandle, IntPtr.Zero);
                return;
            }

            Wlanapi.WLAN_INTERFACE_INFO_LIST interfaceList = (Wlanapi.WLAN_INTERFACE_INFO_LIST)Marshal.PtrToStructure(pInterfaceList, typeof(Wlanapi.WLAN_INTERFACE_INFO_LIST));
            if (interfaceList.dwNumberOfItems > 0)
            {
                Guid interfaceGuid = interfaceList.InterfaceInfo[0].InterfaceGuid;

                IntPtr pAvailableNetworkList;
                result = Wlanapi.WlanGetAvailableNetworkList(hClientHandle, ref interfaceGuid, 0, IntPtr.Zero, out pAvailableNetworkList);
                if (result != 0)
                {
                    Wlanapi.WlanFreeMemory(pInterfaceList);
                    Wlanapi.WlanCloseHandle(hClientHandle, IntPtr.Zero);
                    return;
                }

                Wlanapi.WLAN_AVAILABLE_NETWORK_LIST availableNetworkList = (Wlanapi.WLAN_AVAILABLE_NETWORK_LIST)Marshal.PtrToStructure(pAvailableNetworkList, typeof(Wlanapi.WLAN_AVAILABLE_NETWORK_LIST));
                foreach (Wlanapi.WLAN_AVAILABLE_NETWORK network in availableNetworkList.Network)
                {
                    // 这里可以根据需要修改要连接的网络名称和密码
                    if (network.dot11Ssid.uSSIDLength == 6 && Encoding.ASCII.GetString(network.dot11Ssid.ucSSID, 0, (int)network.dot11Ssid.uSSIDLength) == name)
                    {
                        Wlanapi.WLAN_CONNECTION_PARAMETERS connectionParams = new Wlanapi.WLAN_CONNECTION_PARAMETERS();
                        connectionParams.wlanConnectionMode = Wlanapi.WLAN_CONNECTION_MODE.wlan_connection_mode_profile;
                        connectionParams.strProfile = network.strProfileName;
                        connectionParams.pDot11Ssid = network.dot11Ssid;
                        //connectionParams.pDesiredBssidList = null;
                        connectionParams.dot11BssType = network.dot11BssType;
                        connectionParams.dwFlags = 0;

                        result = Wlanapi.WlanConnect(hClientHandle, ref interfaceGuid, ref connectionParams, IntPtr.Zero);
                        if (result != 0)
                        {
                            Logger.Instance.Error("WlanConnect failed with error: " + result);
                        }
                        else
                        {
                            clientConfig.Wlan = name;
                            Logger.Instance.Info("Successfully connected to the network.");
                        }
                        break;
                    }
                }

                Wlanapi.WlanFreeMemory(pAvailableNetworkList);
            }

            Wlanapi.WlanFreeMemory(pInterfaceList);
            Wlanapi.WlanCloseHandle(hClientHandle, IntPtr.Zero);
        }
    }

    public sealed class WlanReportInfo
    {
        public float Value { get; set; }
        public bool Mute { get; set; }
        public float MasterPeak { get; set; }

        public int HashCode()
        {
            return Value.GetHashCode() ^ Mute.GetHashCode() ^ MasterPeak.GetHashCode();
        }
    }
}
