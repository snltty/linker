using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace linker.libs.winapis
{
    public static class Wlanapi
    {
        [DllImport("Wlanapi.dll", SetLastError = true)]
        public static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("Wlanapi.dll", SetLastError = true)]
        public static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("Wlanapi.dll", SetLastError = true)]
        public static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("Wlanapi.dll", SetLastError = true)]
        public static extern uint WlanGetAvailableNetworkList(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint dwFlags, IntPtr pReserved, out IntPtr ppAvailableNetworkList);

        [DllImport("Wlanapi.dll", SetLastError = true)]
        public static extern uint WlanFreeMemory(IntPtr pMemory);

        [DllImport("Wlanapi.dll", SetLastError = true)]
        public static extern uint WlanConnect(IntPtr hClientHandle, ref Guid pInterfaceGuid, ref WLAN_CONNECTION_PARAMETERS pConnectionParameters, IntPtr pReserved);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_INTERFACE_INFO[] InterfaceInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public WLAN_INTERFACE_STATE isState;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_AVAILABLE_NETWORK_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_AVAILABLE_NETWORK[] Network;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_AVAILABLE_NETWORK
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public DOT11_SSID dot11Ssid;
            public DOT11_BSS_TYPE dot11BssType;
            public uint uNumberOfBssids;
            public bool bNetworkConnectable;
            public uint wlanNotConnectableReason;
            public uint uNumberOfPhyTypes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public DOT11_PHY_TYPE[] dot11PhyTypes;
            public bool bMorePhyTypes;
            public int wlanSignalQuality;
            public bool bSecurityEnabled;
            public DOT11_AUTH_ALGORITHM dot11DefaultAuthAlgorithm;
            public DOT11_CIPHER_ALGORITHM dot11DefaultCipherAlgorithm;
            public uint dwFlags;
            public uint dwReserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;
        }

        public enum WLAN_INTERFACE_STATE
        {
            wlan_interface_state_not_ready,
            wlan_interface_state_connected,
            wlan_interface_state_ad_hoc_network_formed,
            wlan_interface_state_disconnecting,
            wlan_interface_state_disconnected,
            wlan_interface_state_associating,
            wlan_interface_state_discovering,
            wlan_interface_state_authenticating
        }

        public enum DOT11_BSS_TYPE
        {
            dot11_BSS_type_infrastructure = 1,
            dot11_BSS_type_independent = 2,
            dot11_BSS_type_any = 3
        }

        public enum DOT11_PHY_TYPE
        {
            dot11_phy_type_unknown,
            dot11_phy_type_any,
            dot11_phy_type_fhss,
            dot11_phy_type_dsss,
            dot11_phy_type_irbaseband,
            dot11_phy_type_ofdm,
            dot11_phy_type_hrdsss,
            dot11_phy_type_erp,
            dot11_phy_type_ht,
            dot11_phy_type_IHV_start,
            dot11_phy_type_IHV_end
        }

        public enum DOT11_AUTH_ALGORITHM
        {
            DOT11_AUTH_ALGO_80211_OPEN = 1,
            DOT11_AUTH_ALGO_80211_SHARED_KEY = 2,
            DOT11_AUTH_ALGO_WPA = 3,
            DOT11_AUTH_ALGO_WPA_PSK = 4,
            DOT11_AUTH_ALGO_WPA_NONE = 5,
            DOT11_AUTH_ALGO_RSNA = 6,
            DOT11_AUTH_ALGO_RSNA_PSK = 7,
            DOT11_AUTH_ALGO_IHV_START = -2147483648,
            DOT11_AUTH_ALGO_IHV_END = -1
        }

        public enum DOT11_CIPHER_ALGORITHM:uint
        {
            DOT11_CIPHER_ALGO_NONE = 0x00,
            DOT11_CIPHER_ALGO_WEP40 = 0x01,
            DOT11_CIPHER_ALGO_TKIP = 0x02,
            DOT11_CIPHER_ALGO_CCMP = 0x04,
            DOT11_CIPHER_ALGO_WEP104 = 0x05,
            DOT11_CIPHER_ALGO_WPA_USE_GROUP = 0x100,
            DOT11_CIPHER_ALGO_RSN_USE_GROUP = 0x100,
            DOT11_CIPHER_ALGO_WEP = 0x101,
            DOT11_CIPHER_ALGO_IHV_START = 0x80000000,
            DOT11_CIPHER_ALGO_IHV_END = 0xffffffff
        }

        public struct WLAN_CONNECTION_PARAMETERS
        {
            public WLAN_CONNECTION_MODE wlanConnectionMode;
            public string strProfile;
            public DOT11_SSID pDot11Ssid;
            public DOT11_BSSID_LIST pDesiredBssidList;
            public DOT11_BSS_TYPE dot11BssType;
            public uint dwFlags;
        }

        public enum WLAN_CONNECTION_MODE
        {
            wlan_connection_mode_profile,
            wlan_connection_mode_temporary_profile,
            wlan_connection_mode_discovery_secure,
            wlan_connection_mode_discovery_unsecure,
            wlan_connection_mode_auto,
            wlan_connection_mode_invalid
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DOT11_BSSID_LIST
        {
            public uint uNumOfEntries;
            public uint uTotalNumOfEntries;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public DOT11_MAC_ADDRESS[] BSSIDs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DOT11_MAC_ADDRESS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] ucMacAddress;
        }
    }

}
