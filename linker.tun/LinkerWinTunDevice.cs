using linker.libs;
using linker.libs.extends;
using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace linker.tun
{
    internal sealed class LinkerWinTunDevice : ILinkerTunDevice
    {
        private string name = string.Empty;
        public string Name => name;
        public bool Running => session != 0;

        private IntPtr waitHandle = IntPtr.Zero, adapter = IntPtr.Zero, session = IntPtr.Zero, session1 = IntPtr.Zero;
        private Guid guid;
        private int interfaceNumber = 0;
        private IPAddress address;
        private byte prefixLength = 24;

        private CancellationTokenSource tokenSource;

        public LinkerWinTunDevice(string name, Guid guid)
        {
            this.name = name;
            this.guid = guid;
        }

        public bool SetUp(IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
            this.address = address;
            this.prefixLength = prefixLength;

            error = string.Empty;
            if (adapter != 0)
            {
                error = ($"Adapter already exists");
                return false;
            }

            adapter = WintunCreateAdapter(name, name, ref guid);
            if (adapter == 0)
            {
                error = ($"Failed to create adapter {Marshal.GetLastWin32Error():x2}");
                return false;
            }
            uint version = WintunGetRunningDriverVersion();
            session = WintunStartSession(adapter, 0x400000);
            if (session == 0)
            {
                error = ($"Failed to create adapter");
                return false;
            }

            waitHandle = WintunGetReadWaitEvent(session);

            WintunGetAdapterLUID(adapter, out ulong luid);
            {
                MIB_UNICASTIPADDRESS_ROW AddressRow = default;
                InitializeUnicastIpAddressEntry(ref AddressRow);
                AddressRow.sin_family = 2;
                AddressRow.sin_addr = BinaryPrimitives.ReadUInt32LittleEndian(address.GetAddressBytes());
                AddressRow.OnLinkPrefixLength = prefixLength;
                AddressRow.DadState = 4;
                AddressRow.InterfaceLuid = luid;
                uint LastError = CreateUnicastIpAddressEntry(ref AddressRow);
                if (LastError != 0) throw new InvalidOperationException();
            }
            /*
            {
                MIB_IPFORWARD_ROW2 row = default;
                InitializeIpForwardEntry(ref row);
                row.InterfaceLuid = luid;
                row.PrefixLength = 0;
                row.si_family = 2;
                row.NextHop_si_family = 2;
                row.sin_addr = 0;
                row.NextHop_sin_addr = BinaryPrimitives.ReadUInt32LittleEndian(gateway.GetAddressBytes());
                uint LastError = CreateIpForwardEntry2(ref row);
                if (LastError != 0) throw new InvalidOperationException();
            }
            */
            GetWindowsInterfaceNum();

            tokenSource = new CancellationTokenSource();
            return true;
        }
        public void Shutdown()
        {
            tokenSource?.Cancel();
            if (waitHandle != 0)
            {
                SetEvent(waitHandle);
                waitHandle = 0;
            }
            if (session != 0)
            {
                WintunEndSession(session);
                WintunCloseAdapter(adapter);
                WintunDeleteDriver();
            }
            session = 0;
            adapter = 0;
            interfaceNumber = 0;
        }


        public void SetMtu(int value)
        {
            CommandHelper.Windows(string.Empty, new string[] { $"netsh interface ipv4 set subinterface {interfaceNumber}  mtu={value} store=persistent" });
        }
        public void SetNat()
        {
            IPAddress network = NetworkHelper.ToNetworkIp(this.address, NetworkHelper.MaskValue(prefixLength));
            CommandHelper.PowerShell(string.Empty, new string[] { $"New-NetNat -Name {Name} -InternalIPInterfaceAddressPrefix {network}/{prefixLength}" });
        }
        public void RemoveNat()
        {
            CommandHelper.PowerShell(string.Empty, new string[] { $"Remove-NetNat -Name {Name}" });
        }


        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ips.Select(item =>
                {
                    uint maskValue = NetworkHelper.MaskValue(item.Mask);
                    IPAddress mask = NetworkHelper.GetMaskIp(maskValue);
                    IPAddress _ip = NetworkHelper.ToNetworkIp(item.Address, maskValue);

                    return $"route add {_ip} mask {mask} {ip} metric 5 if {interfaceNumber}";
                }).ToArray();
                if (commands.Length > 0)
                {
                    CommandHelper.Windows(string.Empty, commands);
                }
            }
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                uint maskValue = NetworkHelper.MaskValue(item.Mask);
                IPAddress mask = NetworkHelper.GetMaskIp(maskValue);
                IPAddress _ip = NetworkHelper.ToNetworkIp(item.Address, maskValue);
                return $"route delete {_ip}";
            }).ToArray();
            if (commands.Length > 0)
            {
                CommandHelper.Windows(string.Empty, commands.ToArray());
            }
        }


        private byte[] buffer = new byte[2 * 1024];
        public unsafe ReadOnlyMemory<byte> Read()
        {
            for (; tokenSource.IsCancellationRequested == false;)
            {
                IntPtr packet = WintunReceivePacket(session, out var packetSize);

                if (packet != 0)
                {
                    new Span<byte>((byte*)packet, (int)packetSize).CopyTo(buffer.AsSpan(4, (int)packetSize));
                    ((int)packetSize).ToBytes(buffer);
                    WintunReleaseReceivePacket(session, packet);
                    return buffer.AsMemory(0, (int)packetSize + 4);
                }
                else
                {
                    if (Marshal.GetLastWin32Error() == 259L)
                    {
                        WaitForSingleObject(waitHandle, 0xFFFFFFFF);
                    }
                    else
                    {
                        return Helper.EmptyArray;
                    }
                }
            }
            return Helper.EmptyArray;
        }
        public unsafe bool Write(ReadOnlyMemory<byte> buffer)
        {
            if (session == 0 || tokenSource.IsCancellationRequested) return false;

            IntPtr packet = WintunAllocateSendPacket(session, (uint)buffer.Length);
            if (packet != 0)
            {
                buffer.Span.CopyTo(new Span<byte>((byte*)packet, buffer.Length));
                WintunSendPacket(session, packet);
            }
            else
            {
                if (Marshal.GetLastWin32Error() == 111L)
                {
                    return false;
                }
            }
            return true;
        }

        private void GetWindowsInterfaceNum()
        {
            NetworkInterface adapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(c => c.Name == Name);
            if (adapter != null)
            {
                interfaceNumber = adapter.GetIPProperties().GetIPv4Properties().Index;
            }
        }



        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 80)]
        private struct MIB_UNICASTIPADDRESS_ROW
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
        private struct MIB_IPFORWARD_ROW2
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
        private static extern void InitializeUnicastIpAddressEntry(ref MIB_UNICASTIPADDRESS_ROW Row);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint CreateUnicastIpAddressEntry(ref MIB_UNICASTIPADDRESS_ROW Row);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern void InitializeIpForwardEntry(ref MIB_IPFORWARD_ROW2 Row);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint CreateIpForwardEntry2(ref MIB_IPFORWARD_ROW2 Row);
        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        [DllImport("kernel32.dll")]
        public static extern bool SetEvent(IntPtr hEvent);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern IntPtr WintunCreateAdapter(
        [MarshalAs(UnmanagedType.LPWStr)]
        string name,
        [MarshalAs(UnmanagedType.LPWStr)]
        string tunnelType,
        ref Guid guid);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern uint WintunGetRunningDriverVersion();

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern void WintunGetAdapterLUID(IntPtr adapter, out ulong luid);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern IntPtr WintunStartSession(IntPtr adapter, uint capacity);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern IntPtr WintunGetReadWaitEvent(IntPtr session);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern IntPtr WintunReceivePacket(IntPtr session, out uint packetSize);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern void WintunSendPacket(IntPtr session, IntPtr packet);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern void WintunEndSession(IntPtr session);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern void WintunCloseAdapter(IntPtr adapter);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern IntPtr WintunAllocateSendPacket(IntPtr session, uint packetSize);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern IntPtr WintunOpenAdapter(
            [MarshalAs(UnmanagedType.LPWStr)]
        string name);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern bool WintunDeleteDriver();

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern void WintunReleaseReceivePacket(IntPtr session, IntPtr packet);

        [DllImport("wintun.dll", SetLastError = true)]
        private static extern void WintunSetLogger(WINTUN_LOGGER_CALLBACK newLogger);

        private delegate void WINTUN_LOGGER_CALLBACK(
            WINTUN_LOGGER_LEVEL level,
            ulong timestamp,
            [MarshalAs(UnmanagedType.LPWStr)]
        string message);

        private enum WINTUN_LOGGER_LEVEL
        {
            WINTUN_LOG_INFO, /**< Informational */
            WINTUN_LOG_WARN, /**< Warning */
            WINTUN_LOG_ERR   /**< Error */
        }


    }
}
