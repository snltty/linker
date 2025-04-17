﻿using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace linker.tun
{
    [SupportedOSPlatform("windows")]
    internal sealed class LinkerWinTunDevice : ILinkerTunDevice
    {
        private string name = string.Empty;
        public string Name => name;
        public bool Running => session != 0;

        private IntPtr waitHandle = IntPtr.Zero, adapter = IntPtr.Zero, session = IntPtr.Zero;
        private int interfaceNumber = 0;
        private IPAddress address;
        private uint address32;
        private byte prefixLength = 24;

        private string defaultInterfaceName = string.Empty;
        private int defaultInterfaceNumber = 0;

        private CancellationTokenSource tokenSource;

        private WinDivert winDivert;

        public LinkerWinTunDevice()
        {
            /*
            winDivert = new WinDivert("inbound and (( ip.SrcAddr == 10.18.18.0/24 and ip.DstAddr == 192.168.56.0/24) or ( ip.SrcAddr == 192.168.56.0/24))", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff);
            var packet = new Memory<byte>(new byte[WinDivert.MTUMax]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[1]);
            TimerHelper.Async(() =>
            {
                uint recvLen = 0, addrLen = 0;
                while (true)
                {
                    try
                    {
                        (recvLen, addrLen) = winDivert.RecvEx(packet.Span, abuf.Span);

                        var recv = packet[..(int)recvLen];
                        var addr = abuf[..(int)addrLen];
                        foreach (var (i, p) in new WinDivertIndexedPacketParser(recv))
                        {
                            ModifyPacket(p, ref addr.Span[i]);
                        }
                        //_ = winDivert.SendEx(recv.Span, addr.Span);
                    }
                    catch (Exception)
                    {
                        winDivert.Dispose();
                        break;
                    }

                }
            });
            */
        }
        NetworkIPv4Addr sourceAddr = IPv4Addr.Parse("10.18.18.23");
        private unsafe void ModifyPacket(WinDivertParseResult p, ref WinDivertAddress addr)
        {
            if (p.IPv4Hdr->SrcAddr == sourceAddr)
            {
                Console.WriteLine($"{p.IPv4Hdr->SrcAddr}->{p.IPv4Hdr->DstAddr}");
            }
            //WinDivert.CalcChecksums(p.Packet.Span, ref addr, 0);
        }

        public bool Setup(string name, IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
            this.name = name;
            this.address = address;
            this.address32 = NetworkHelper.ToValue(address);
            this.prefixLength = prefixLength;

            error = string.Empty;
            if (adapter != 0)
            {
                error = ($"Adapter already exists");
                return false;
            }
            Guid guid = Guid.Parse("771EF382-8718-5BC5-EBF0-A28B86142278");
            adapter = WinTun.WintunCreateAdapter(name, name, ref guid);
            if (adapter == 0)
            {
                adapter = WinTun.WintunOpenAdapter(name);
                if (adapter == 0)
                {
                    error = ($"Failed to create adapter {Marshal.GetLastWin32Error()}");
                    Shutdown();
                    return false;
                }
            }
            uint version = WinTun.WintunGetRunningDriverVersion();
            session = WinTun.WintunStartSession(adapter, 0x400000);
            if (session == 0)
            {
                error = ($"Failed to start session");
                Shutdown();
                return false;
            }
            waitHandle = WinTun.WintunGetReadWaitEvent(session);

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    AddIPV4();
                    AddIPV6();

                    GetWindowsInterfaceNum();
                    GetDefaultInterface();
                    tokenSource = new CancellationTokenSource();

                    return true;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }
            error = ($"Failed to set adapter ip {Marshal.GetLastWin32Error()}");
            Shutdown();
            return false;

        }

        private void AddIPV4()
        {
            try
            {
                if (session == 0) return;

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
            }
            catch (Exception)
            {
            }
        }
        private void AddIPV6()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == Name);
            if (networkInterface != null)
            {
                var commands = networkInterface.GetIPProperties()
                    .UnicastAddresses
                    .Where(c => c.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    .Select(c => new IPAddress(c.Address.GetAddressBytes(), 0))
                    .Select(c => $"netsh interface ipv6 delete address \"{Name}\" address=\"{c}\"").ToList();

                byte[] ipv6 = IPAddress.Parse("fe80::1818:1818:1818:1818").GetAddressBytes();
                address.GetAddressBytes().CopyTo(ipv6, ipv6.Length - 4);
                commands.Add($"netsh interface ipv6 add address \"{Name}\" address=\"{new IPAddress(ipv6)}\"");
                CommandHelper.Windows(string.Empty, [.. commands]);
            }
        }

        public void Shutdown()
        {
            tokenSource?.Cancel();
            if (waitHandle != 0)
            {
                WinTun.SetEvent(waitHandle);
            }
            if (session != 0)
            {
                WinTun.WintunEndSession(session);
            }
            if (adapter != 0)
            {
                WinTun.WintunCloseAdapter(adapter);
            }
            waitHandle = 0;
            session = 0;
            adapter = 0;
            interfaceNumber = 0;
        }

        public void Refresh()
        {
            if (session == 0) return;
            try
            {
                IntPtr oldSession = session;
                IntPtr oldWaitHandle = waitHandle;

                CommandHelper.Windows(string.Empty, new string[] { $"netsh interface set interface {Name} enable" });
                session = WinTun.WintunStartSession(adapter, 0x400000);
                waitHandle = WinTun.WintunGetReadWaitEvent(session);
                AddIPV4();
                AddIPV6();

                WinTun.SetEvent(oldWaitHandle);
                WinTun.WintunEndSession(oldSession);
            }
            catch (Exception)
            {
            }
        }

        public void SetMtu(int value)
        {
            CommandHelper.Windows(string.Empty, new string[] {
                $"netsh interface ipv4 set subinterface {interfaceNumber}  mtu={value} store=persistent" ,
                 $"netsh interface ipv6 set subinterface {interfaceNumber}  mtu={value} store=persistent"
            });
        }
        public void SetNat(out string error)
        {

            error = string.Empty;
            //return;

            if (address == null || address.Equals(IPAddress.Any)) return;
            try
            {
                CommandHelper.PowerShell($"start-service WinNat", [], out error);
                CommandHelper.PowerShell($"Install-WindowsFeature -Name Routing -IncludeManagementTools", [], out error);
                CommandHelper.PowerShell($"Set-ItemProperty -Path \"HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" -Name \"IPEnableRouter\" -Value 1", [], out error);

                IPAddress network = NetworkHelper.ToNetworkIP(this.address, NetworkHelper.ToPrefixValue(prefixLength));
                CommandHelper.PowerShell($"Remove-NetNat -Name {Name} -Confirm:$false", [], out error);
                CommandHelper.PowerShell($"New-NetNat -Name {Name} -InternalIPInterfaceAddressPrefix {network}/{prefixLength}", [], out error);

                if (string.IsNullOrWhiteSpace(error))
                {
                    return;
                }
                /*
                CommandHelper.Windows(string.Empty, [$"net start SharedAccess"]);
                string result = CommandHelper.Windows(string.Empty, [$"linker.ics.exe {defaultInterfaceName} {Name} enable"]);
                if (result.Contains($"enable success"))
                {
                    return;
                }
                */
                error = "NetNat and ICS not supported";
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }
        public void RemoveNat(out string error)
        {
            error = string.Empty;
            if (address == null || address.Equals(IPAddress.Any)) return;

            try
            {
                CommandHelper.PowerShell($"start-service WinNat", [], out error);
                CommandHelper.PowerShell($"Remove-NetNat -Name {Name} -Confirm:$false", [], out error);
                //CommandHelper.Windows(string.Empty, [$"linker.ics.exe {defaultInterfaceName} {Name} disable"]);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }


        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            string str = CommandHelper.Windows(string.Empty, new string[] { $"netsh interface portproxy show v4tov4" });
            IEnumerable<LinkerTunDeviceForwardItem> lines = str.Split(Environment.NewLine)
                .Select(c => Regex.Replace(c, @"\s+", " ").Split(' '))
                .Where(c => c.Length > 0 && c[0] == "0.0.0.0")
                .Select(c =>
                {
                    IPEndPoint dist = IPEndPoint.Parse($"{c[2]}:{c[3]}");
                    int port = int.Parse(c[1]);
                    return new LinkerTunDeviceForwardItem { ListenAddr = IPAddress.Any, ListenPort = port, ConnectAddr = dist.Address, ConnectPort = dist.Port };
                });
            return lines.ToList();
        }
        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            string[] commands = forwards.Where(c => c != null && c.Enable).Select(c =>
            {
                return $"netsh interface portproxy add v4tov4 listenaddress={c.ListenAddr} listenport={c.ListenPort} connectaddress={c.ConnectAddr} connectport={c.ConnectPort}";
            }).ToArray();
            if (commands.Length > 0)
                CommandHelper.Windows(string.Empty, commands);
        }
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            string[] commands = forwards.Where(c => c != null && c.Enable).Select(c =>
            {
                return $"netsh interface portproxy delete v4tov4 listenport={c.ListenPort} listenaddress={c.ListenAddr}";
            }).ToArray();
            if (commands.Length > 0)
                CommandHelper.Windows(string.Empty, commands);
        }


        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            if (interfaceNumber > 0)
            {
                string[] commands = ips.Select(item =>
                {
                    uint maskValue = NetworkHelper.ToPrefixValue(item.PrefixLength);
                    IPAddress mask = NetworkHelper.ToIP(maskValue);
                    IPAddress _ip = NetworkHelper.ToNetworkIP(item.Address, maskValue);

                    return $"route add {_ip} mask {mask} {ip} metric 5 if {interfaceNumber}";
                }).ToArray();
                if (commands.Length > 0)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        LoggerHelper.Instance.Warning($"tuntap win add route: {string.Join("\r\n", commands)}");
                    CommandHelper.Windows(string.Empty, commands);
                }
            }
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ip)
        {
            string[] commands = ip.Select(item =>
            {
                uint maskValue = NetworkHelper.ToPrefixValue(item.PrefixLength);
                IPAddress mask = NetworkHelper.ToIP(maskValue);
                IPAddress _ip = NetworkHelper.ToNetworkIP(item.Address, maskValue);
                return $"route delete {_ip}";
            }).ToArray();
            if (commands.Length > 0)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning($"tuntap win del route: {string.Join("\r\n", commands)}");
                CommandHelper.Windows(string.Empty, commands.ToArray());
            }
        }


        private byte[] buffer = new byte[8 * 1024];
        public unsafe byte[] Read(out int length)
        {
            length = 0;
            if (session == 0) return Helper.EmptyArray;
            for (; tokenSource.IsCancellationRequested == false;)
            {
                IntPtr packet = WinTun.WintunReceivePacket(session, out uint size);
                length = (int)size;

                if (packet != 0)
                {
                    new Span<byte>((byte*)packet, length).CopyTo(buffer.AsSpan(4, length));
                    length.ToBytes(buffer);
                    WinTun.WintunReleaseReceivePacket(session, packet);
                    length += 4;
                    return buffer;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();

                    if (error == 0 || error == 259L)
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
            /*
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) == 4)
            {
                uint distIP = BinaryPrimitives.ReadUInt32BigEndian(buffer.Span.Slice(16, 4));
                if (distIP != address32)
                {
                    var addr = new WinDivertAddress
                    {
                        Layer = WinDivert.Layer.Network,
                        Outbound = true,
                        IPv6 = false
                    };
                    winDivert.SendEx(buffer.Span, new ReadOnlySpan<WinDivertAddress>(ref addr));
                    Console.WriteLine($"WinDivert sendto {string.Join(".", buffer.Span.Slice(16, 4).ToArray())}");
                    return true;
                }
            }
            */

            IntPtr packet = WinTun.WintunAllocateSendPacket(session, (uint)buffer.Length);
            if (packet != 0)
            {
                buffer.Span.CopyTo(new Span<byte>((byte*)packet, buffer.Length));
                WinTun.WintunSendPacket(session, packet);
                return true;
            }
            else
            {
                if (Marshal.GetLastWin32Error() == 111L)
                {
                    return false;
                }
            }
            return false;
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
        private void GetDefaultInterface()
        {
            string[] lines = CommandHelper.Windows(string.Empty, new string[] { $"route print" }).Split(Environment.NewLine);
            foreach (var item in lines)
            {
                if (item.Trim().StartsWith("0.0.0.0"))
                {
                    string[] arr = Regex.Replace(item.Trim(), @"\s+", " ").Split(' ');
                    IPAddress ip = IPAddress.Parse(arr[arr.Length - 2]);

                    foreach (var inter in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        try
                        {
                            if (ip.Equals(inter.GetIPProperties().UnicastAddresses.FirstOrDefault(c => c.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Address))
                            {
                                defaultInterfaceName = inter.Name;
                                defaultInterfaceNumber = inter.GetIPProperties().GetIPv4Properties().Index;
                                return;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        public async Task<bool> CheckAvailable(bool order = false)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            if (order)
                InterfaceOrder(interfaces);
            NetworkInterface networkInterface = interfaces.FirstOrDefault(c => c.Name == Name || c.Description == $"{Name} Tunnel" || c.Name.Contains(Name));

            UnicastIPAddressInformation firstIpv4 = networkInterface?.GetIPProperties()
                .UnicastAddresses.FirstOrDefault(c => c.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (networkInterface == null || firstIpv4 == null || firstIpv4.Address == null || firstIpv4.Address.Equals(address) == false)
            {
                return false;
            }
            return await Task.FromResult(true);
        }
        private void InterfaceOrder(NetworkInterface[] interfaces)
        {
            NetworkInterface linker = interfaces.FirstOrDefault(c => c.Name == Name || c.Description == $"{Name} Tunnel" || c.Name.Contains(Name));
            NetworkInterface first = interfaces.FirstOrDefault();

            if (linker != null && linker.Name != first.Name)
            {
                UnicastIPAddressInformation firstIpv4 = linker.GetIPProperties().UnicastAddresses.FirstOrDefault(c => c.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (firstIpv4 == null || firstIpv4.Address == null || firstIpv4.Address.Equals(address) == false)
                {
                    return;
                }


                int metricv4 = 0;
                int metricv6 = 0;
                List<string> commands = new List<string> {
                    $"netsh interface ipv4 set interface \"{Name}\" metric={++metricv4}",
                    $"netsh interface ipv6 set interface \"{Name}\" metric={++metricv6}"
                };
                commands.AddRange(interfaces
                    .Where(c => c.Name != Name)
                    .Select(c => $"netsh interface ipv4 set interface \"{c.Name}\" metric={++metricv4}"));
                commands.AddRange(interfaces
                    .Where(c => c.Name != Name)
                    .Select(c => $"netsh interface ipv6 set interface \"{c.Name}\" metric={++metricv6}"));
                commands.Add(string.Empty);
                foreach (var command in commands)
                {
                    CommandHelper.Windows(string.Empty, [command]);
                }
            }
        }

    }
}


