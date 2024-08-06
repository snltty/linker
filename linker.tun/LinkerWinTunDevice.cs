using linker.libs;
using linker.libs.extends;
using linker.libs.winapis;
using Microsoft.Win32;
using System.Buffers.Binary;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace linker.tun
{
    [SupportedOSPlatform("windows")]
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

        public bool Setup(IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
            this.address = address;
            this.prefixLength = prefixLength;

            error = string.Empty;
            if (adapter != 0)
            {
                error = ($"Adapter already exists");
                return false;
            }

            adapter = WinTun.WintunCreateAdapter(name, name, ref guid);
            if (adapter == 0)
            {
                error = ($"Failed to create adapter {Marshal.GetLastWin32Error():x2}");
                return false;
            }
            uint version = WinTun.WintunGetRunningDriverVersion();
            session = WinTun.WintunStartSession(adapter, 0x400000);
            if (session == 0)
            {
                error = ($"Failed to start session");
                return false;
            }

            waitHandle = WinTun.WintunGetReadWaitEvent(session);

            WinTun.WintunGetAdapterLUID(adapter, out ulong luid);
            {
                WinTun.MIB_UNICASTIPADDRESS_ROW AddressRow = default;
                WinTun.InitializeUnicastIpAddressEntry(ref AddressRow);
                AddressRow.sin_family = 2;
                AddressRow.sin_addr = BinaryPrimitives.ReadUInt32LittleEndian(address.GetAddressBytes());
                AddressRow.OnLinkPrefixLength = prefixLength;
                AddressRow.DadState = 4;
                AddressRow.InterfaceLuid = luid;
                uint LastError = WinTun.CreateUnicastIpAddressEntry(ref AddressRow);
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
                WinTun.SetEvent(waitHandle);
                waitHandle = 0;
            }
            if (session != 0)
            {
                WinTun.WintunEndSession(session);
                WinTun.WintunCloseAdapter(adapter);
            }
            session = 0;
            adapter = 0;
            interfaceNumber = 0;
        }


        public void SetMtu(int value)
        {
            CommandHelper.Windows(string.Empty, new string[] { $"netsh interface ipv4 set subinterface {interfaceNumber}  mtu={value} store=persistent" });
        }
        public void SetNat(out string error)
        {
            error = string.Empty;
            try
            {
                CommandHelper.PowerShell($"start-service WinNat", []);
                IPAddress network = NetworkHelper.ToNetworkIp(this.address, NetworkHelper.MaskValue(prefixLength));
                CommandHelper.PowerShell($"New-NetNat -Name {Name} -InternalIPInterfaceAddressPrefix {network}/{prefixLength}", []);

                try
                {
                    var scope = new ManagementScope(@"root\StandardCimv2");

                    using var netNatClass = new ManagementClass($"{scope.Path}:MSFT_NetNat");
                    using var netNat = netNatClass.CreateInstance();
                    netNat.Properties["Name"].Value = Name;
                    netNat.Properties["Active"].Value = true;
                    netNat.Properties["InternalIPInterfaceAddressPrefix"].Value = $"{network}/{prefixLength}";
                    netNat.Put();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }
        public void RemoveNat(out string error)
        {
            error = string.Empty;

            try
            {
                CommandHelper.PowerShell($"start-service WinNat", []);
                CommandHelper.PowerShell($"Remove-NetNat -Name {Name} -Confirm:$false", []);
                
                try
                {
                    var scope = new ManagementScope(@"root\StandardCimv2");
                    var query = new ObjectQuery("SELECT * FROM MSFT_NetNat");
                    using var searcher = new ManagementObjectSearcher(scope, query);
                    using var natObjects = searcher.Get();
                    foreach (ManagementObject natObject in natObjects)
                    {
                        var name = (string)natObject["Name"];
                        if (name == Name)
                        {
                            natObject.Delete();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip, bool gateway)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ips.Select(item =>
                {
                    uint maskValue = NetworkHelper.MaskValue(item.PrefixLength);
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
        public void DelRoute(LinkerTunDeviceRouteItem[] ip, bool gateway)
        {
            string[] commands = ip.Select(item =>
            {
                uint maskValue = NetworkHelper.MaskValue(item.PrefixLength);
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
                IntPtr packet = WinTun.WintunReceivePacket(session, out var packetSize);

                if (packet != 0)
                {
                    new Span<byte>((byte*)packet, (int)packetSize).CopyTo(buffer.AsSpan(4, (int)packetSize));
                    ((int)packetSize).ToBytes(buffer);
                    WinTun.WintunReleaseReceivePacket(session, packet);
                    return buffer.AsMemory(0, (int)packetSize + 4);
                }
                else
                {
                    if (Marshal.GetLastWin32Error() == 259L)
                    {
                        WinTun.WaitForSingleObject(waitHandle, 0xFFFFFFFF);
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

            IntPtr packet = WinTun.WintunAllocateSendPacket(session, (uint)buffer.Length);
            if (packet != 0)
            {
                buffer.Span.CopyTo(new Span<byte>((byte*)packet, buffer.Length));
                WinTun.WintunSendPacket(session, packet);
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

        public void Clear()
        {
            ClearRegistry();
        }
        private void ClearRegistry()
        {
            string[] delValues = [Name];
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles");
                foreach (var item in key.GetSubKeyNames())
                {
                    RegistryKey itemKey = key.OpenSubKey(item);
                    string value = itemKey.GetValue("Description", string.Empty).ToString();
                    itemKey.Close();
                    if (delValues.Contains(value))
                    {
                        try
                        {
                            Registry.LocalMachine.DeleteSubKey($"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Profiles\\{item}");
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                key.Close();

                key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Signatures\\Unmanaged");
                foreach (var item in key.GetSubKeyNames())
                {
                    RegistryKey itemKey = key.OpenSubKey(item);
                    string value = itemKey.GetValue("Description", string.Empty).ToString();
                    itemKey.Close();
                    if (delValues.Any(c => value.Contains($"{c} ") || value == c))
                    {
                        try
                        {
                            Registry.LocalMachine.DeleteSubKey($"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\NetworkList\\Signatures\\Unmanaged\\{item}");
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                key.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
