using System.Runtime.InteropServices;

namespace linker.tun.device
{
    internal static class WinTun
    {


        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 80)]
        internal struct MIB_UNICASTIPADDRESS_ROW
        {
            [FieldOffset(0)]
            public ushort sin_family;
            [FieldOffset(4)]
            public uint sin_addr;
            [FieldOffset(32)]
            public ulong InterfaceLuid;
            [FieldOffset(60)]
            public byte OnLinkPrefixLength;
            [FieldOffset(64)]
            public int DadState;
        }


        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 104)]
        internal struct MIB_IPFORWARD_ROW2
        {
            [FieldOffset(0)]
            public ulong InterfaceLuid;
            [FieldOffset(12)]
            public ushort si_family;
            [FieldOffset(16)]
            public uint sin_addr;
            [FieldOffset(40)]
            public byte PrefixLength;
            [FieldOffset(48)]
            public uint NextHop_sin_addr;
            [FieldOffset(44)]
            public ushort NextHop_si_family;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern void InitializeUnicastIpAddressEntry(ref MIB_UNICASTIPADDRESS_ROW Row);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern uint CreateUnicastIpAddressEntry(ref MIB_UNICASTIPADDRESS_ROW Row);


        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern void InitializeIpForwardEntry(ref MIB_IPFORWARD_ROW2 Row);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern uint CreateIpForwardEntry2(ref MIB_IPFORWARD_ROW2 Row);
        [DllImport("kernel32.dll")]
        internal static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);
        [DllImport("kernel32.dll")]
        internal static extern bool SetEvent(nint hEvent);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern nint WintunCreateAdapter(
        [MarshalAs(UnmanagedType.LPWStr)]
        string name,
        [MarshalAs(UnmanagedType.LPWStr)]
        string tunnelType,
        ref Guid guid);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern uint WintunGetRunningDriverVersion();

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern void WintunGetAdapterLUID(nint adapter, out ulong luid);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern nint WintunStartSession(nint adapter, uint capacity);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern nint WintunGetReadWaitEvent(nint session);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern nint WintunReceivePacket(nint session, out uint packetSize);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern void WintunSendPacket(nint session, nint packet);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern void WintunEndSession(nint session);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern void WintunCloseAdapter(nint adapter);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern nint WintunAllocateSendPacket(nint session, uint packetSize);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern nint WintunOpenAdapter(
            [MarshalAs(UnmanagedType.LPWStr)]
        string name);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern bool WintunDeleteDriver();

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern void WintunReleaseReceivePacket(nint session, nint packet);

        [DllImport("wintun.dll", SetLastError = true)]
        internal static extern void WintunSetLogger(WINTUN_LOGGER_CALLBACK newLogger);

        internal delegate void WINTUN_LOGGER_CALLBACK(
            WINTUN_LOGGER_LEVEL level,
            ulong timestamp,
            [MarshalAs(UnmanagedType.LPWStr)]
        string message);

        internal enum WINTUN_LOGGER_LEVEL
        {
            WINTUN_LOG_INFO, /**< Informational */
            WINTUN_LOG_WARN, /**< Warning */
            WINTUN_LOG_ERR   /**< Error */
        }

    }
}
