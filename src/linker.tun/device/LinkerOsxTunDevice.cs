using linker.libs;
using linker.libs.extends;
using Microsoft.Win32.SafeHandles;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace linker.tun.device
{
    /// <summary>
    /// macOS network adapter implementation
    /// </summary>
    internal sealed class LinkerOsxTunDevice : ILinkerTunDevice
    {
        private string name = string.Empty;
        public string Name => name;
        public bool Running => safeFileHandle != null;

        private string interfaceMac = string.Empty;
        private FileStream fsRead = null;
        private FileStream fsWrite = null;
        private SafeFileHandle safeFileHandle;
        private IPAddress address;
        private byte prefixLength = 24;
        private int tunUnit = -1;

        public LinkerOsxTunDevice()
        {
        }

        public bool Setup(LinkerTunDeviceSetupInfo info, out string error)
        {
            error = string.Empty;

            this.name = info.Name;
            this.address = info.Address;
            this.prefixLength = info.PrefixLength;

            if (Running)
            {
                error = ($"Adapter already exists");
                return false;
            }

            if (OpenUtunDevice(out error) == false)
            {
                return false;
            }

            if (ConfigureInterface(out error) == false)
            {
                Shutdown();
                return false;
            }

            fsRead = new FileStream(safeFileHandle, FileAccess.Read, 65 * 1024, false);
            fsWrite = new FileStream(safeFileHandle, FileAccess.Write, 65 * 1024, false);

            return true;
        }

        private bool OpenUtunDevice(out string error)
        {
            error = string.Empty;

            try
            {
                // On macOS, utun devices are created automatically with unit numbers
                // Using -1 lets the system assign a free unit
                IntPtr ifnameBuffer = Marshal.AllocHGlobal(256);

                try
                {
                    int fd = OsxAPI.open_utun(-1, ifnameBuffer, new UIntPtr(256), out int errno);

                    if (fd < 0)
                    {
                        error = $"Failed to open utun device. Error: {errno}";
                        return false;
                    }

                    // Retrieve interface name
                    interfaceMac = Marshal.PtrToStringAnsi(ifnameBuffer);
                    if (string.IsNullOrEmpty(interfaceMac))
                    {
                        error = "Failed to get interface name";
                        return false;
                    }

                    // Extract unit number (e.g. utun5 -> 5)
                    var match = Regex.Match(interfaceMac, @"utun(\d+)");
                    if (match.Success)
                    {
                        tunUnit = int.Parse(match.Groups[1].Value);
                    }

                    // Create SafeFileHandle
                    safeFileHandle = new SafeFileHandle(new IntPtr(fd), true);

                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(ifnameBuffer);
                }
            }
            catch (Exception ex)
            {
                error = $"Exception opening utun device: {ex.Message}";
                return false;
            }
        }

        private bool ConfigureInterface(out string error)
        {
            error = string.Empty;

            try
            {
                Span<byte> gatewayBytes = stackalloc byte[4];
                address.TryWriteBytes(gatewayBytes, out _);

                // On macOS, the TUN interface gateway IP (usually .1)
                gatewayBytes[3] = 1; // Set last octet to 1 (e.g., 10.18.18.1)
                IPAddress gatewayAddr = new IPAddress(gatewayBytes);

                IPAddress networkAddr = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));

                string[] commands = new string[]
                {
                    // Configure interface - use gateway as destination
                    $"sudo ifconfig {interfaceMac} {address} {gatewayAddr} netmask 255.255.255.255 up",
                    $"sudo ifconfig {interfaceMac} mtu 1420",
                    
                    // Enable IP forwarding
                    "sudo sysctl -w net.inet.ip.forwarding=1",
                    "sudo sysctl -w net.inet.ip.redirect=0",
                    
                    // Remove old routes
                    $"sudo route delete -net {networkAddr}/{prefixLength} 2>/dev/null || true",
                    
                    // Add network route via interface
                    $"sudo route add -net {networkAddr}/{prefixLength} -interface {interfaceMac}",
                    
                    // Add host route for self
                    $"sudo route add -host {address} -interface {interfaceMac}",
                    
                    // Add gateway route
                    $"sudo route add -host {gatewayAddr} -interface {interfaceMac}"
                };

                string result = CommandHelper.Osx(string.Empty, commands, out error);

                // Ignore non-critical routing errors
                if (!string.IsNullOrEmpty(error))
                {
                    if (!(!error.Contains("File exists") && !error.Contains("Network is unreachable") && !error.Contains("route: writing to routing socket")))
                    {
                        // Continue for non-critical issues
                        error = string.Empty;
                    }
                }

                // Verify interface is UP
                result = CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {interfaceMac}" });
                if (!result.Contains("UP"))
                {
                    error = "Failed to bring interface up";
                    return false;
                }

                string routes = CommandHelper.Osx(string.Empty, new string[] { "netstat -rn | grep " + interfaceMac });

                return true;
            }
            catch (Exception ex)
            {
                error = $"Exception configuring interface: {ex.Message}";
                return false;
            }
        }

        public void Shutdown()
        {
            try
            {
                if (!string.IsNullOrEmpty(interfaceMac))
                {
                    // Bring interface down
                    CommandHelper.Osx(string.Empty, new string[] { $"sudo ifconfig {interfaceMac} down" });
                }

                safeFileHandle?.Dispose();
                safeFileHandle = null;

                try { fsRead?.Flush(); } catch (Exception) { }
                try { fsRead?.Close(); fsRead?.Dispose(); } catch (Exception) { }
                fsRead = null;

                try { fsWrite?.Flush(); } catch (Exception) { }
                try { fsWrite?.Close(); fsWrite?.Dispose(); } catch (Exception) { }
                fsWrite = null;
            }
            catch (Exception)
            {
            }

            interfaceMac = string.Empty;
            tunUnit = -1;
            GC.Collect();
        }

        public void Refresh()
        {
            if (safeFileHandle == null) return;
            try
            {
                CommandHelper.Osx(string.Empty, new string[] {
                    $"sudo ifconfig {interfaceMac} up"
                });
            }
            catch (Exception)
            {
            }
        }

        public void SetMssFix(int value = 0)
        {

        }
        public void SetMtu(int value)
        {
            if (!string.IsNullOrEmpty(interfaceMac))
            {
                CommandHelper.Osx(string.Empty, new string[] { $"sudo ifconfig {interfaceMac} mtu {value}" });
            }
        }

        private string GetDefaultInterface()
        {
            return CommandHelper.Osx(string.Empty, new string[] { "route get default | grep interface | awk '{print $2}'" });
        }

        public void SetNat(out string error)
        {
            error = string.Empty;
            if (address == null || address.Equals(IPAddress.Any)) return;

            try
            {
                IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));
                string defaultInterface = GetDefaultInterface().Trim();

                if (string.IsNullOrEmpty(defaultInterface))
                {
                    // Fallback - try en0 or eth0
                    defaultInterface = "en0";
                }

                // Check pfctl status
                string pfStatus = CommandHelper.Osx(string.Empty, new string[] { "sudo pfctl -s info" });

                // Basic NAT rules
                string pfRules = $@"# VPN NAT Rules
# Enable packet forwarding
set skip on lo0

# NAT outgoing traffic from VPN network
nat on {defaultInterface} from {network}/{prefixLength} to any -> ({defaultInterface})

# Allow traffic on TUN interface  
pass on {interfaceMac} all

# Allow forwarding from VPN network
pass from {network}/{prefixLength} to any keep state
pass from any to {network}/{prefixLength} keep state

# Allow ICMP (for ping)
pass inet proto icmp all
";

                string tempFile = "/tmp/vpn_pf_rules";
                File.WriteAllText(tempFile, pfRules);

                // Enable IP forwarding
                CommandHelper.Osx(string.Empty, new string[] {
                    "sudo sysctl -w net.inet.ip.forwarding=1"
                });

                // Load pfctl rules
                string pfResult = CommandHelper.Osx(string.Empty, new string[] {
                    $"sudo pfctl -f {tempFile}",
                    "sudo pfctl -e"
                }, out error);

                try { File.Delete(tempFile); } catch { }

                // Verify pfctl state
                string rules = CommandHelper.Osx(string.Empty, new string[] { "sudo pfctl -s nat" });
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
                // Disable pfctl
                CommandHelper.Osx(string.Empty, new string[] {
                    "sudo pfctl -d"
                }, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            // On macOS, port forwarding is generally handled with pfctl
            // Simple implementation - real-world parsing may be more complex
            var forwards = new List<LinkerTunDeviceForwardItem>();

            try
            {
                string result = CommandHelper.Osx(string.Empty, new string[] { "sudo pfctl -s nat" });
                // Could parse pfctl output using regex if needed
            }
            catch (Exception)
            {
            }

            return forwards;
        }

        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            if (forwards == null || forwards.Count == 0) return;

            try
            {
                string defaultInterface = GetDefaultInterface().Trim();
                List<string> rules = new List<string>();

                foreach (var forward in forwards.Where(f => f != null && f.Enable))
                {
                    rules.Add($"rdr on {defaultInterface} inet proto tcp from any to any port {forward.ListenPort} -> {forward.ConnectAddr} port {forward.ConnectPort}");
                }

                if (rules.Count > 0)
                {
                    string tempFile = "/tmp/vpn_forward_rules";
                    File.WriteAllText(tempFile, string.Join("\n", rules));

                    CommandHelper.Osx(string.Empty, new string[] {
                        $"sudo pfctl -f {tempFile}",
                        "sudo pfctl -e"
                    });

                    try { File.Delete(tempFile); } catch { }
                }
            }
            catch (Exception)
            {
            }
        }

        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            // Removing pfctl rules usually requires reloading configuration
            try
            {
                CommandHelper.Osx(string.Empty, new string[] { "sudo pfctl -F nat" });
            }
            catch (Exception)
            {
            }
        }

        public void AddRoute(LinkerTunDeviceRouteItem[] routes)
        {
            if (routes == null || routes.Length == 0) return;

            string[] commands = routes.Select(route =>
            {
                uint prefixValue = NetworkHelper.ToPrefixValue(route.PrefixLength);
                IPAddress network = NetworkHelper.ToNetworkIP(route.Address, prefixValue);
                return $"sudo route add -net {network}/{route.PrefixLength} -interface {interfaceMac}";
            }).ToArray();

            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands);
            }
        }

        public void RemoveRoute(LinkerTunDeviceRouteItem[] routes)
        {
            if (routes == null || routes.Length == 0) return;

            string[] commands = routes.Select(route =>
            {
                uint prefixValue = NetworkHelper.ToPrefixValue(route.PrefixLength);
                IPAddress network = NetworkHelper.ToNetworkIP(route.Address, prefixValue);
                return $"sudo route delete -net {network}/{route.PrefixLength}";
            }).ToArray();

            if (commands.Length > 0)
            {
                CommandHelper.Osx(string.Empty, commands);
            }
        }

        private readonly byte[] buffer = new byte[65 * 1024];
        private readonly object writeLockObj = new object();

        public byte[] Read(out uint length)
        {
            length = 0;
            if (safeFileHandle == null || fsRead == null)
            {
                return Helper.EmptyArray;
            }

            length = (uint)fsRead.Read(buffer.AsSpan(4));
            ((ushort)(length + 2)).ToBytes(buffer.AsSpan());
            buffer[2] = 0;
            buffer[3] = 0;

            length += 4;
            return buffer;
        }

        public bool Write(ReadOnlyMemory<byte> packet)
        {
            if (safeFileHandle == null || fsWrite == null)
            {
                return false;
            }

            lock (writeLockObj)
            {
                try
                {
                    fsWrite.Write(packet.Span);
                    fsWrite.Flush();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public Task<bool> CheckAvailable(bool order = false)
        {
            if (string.IsNullOrEmpty(interfaceMac))
                return Task.FromResult(false);

            try
            {
                string output = CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {interfaceMac}" });
                return Task.FromResult(output.Contains("UP") && output.Contains(address.ToString()));
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }
    }


}
