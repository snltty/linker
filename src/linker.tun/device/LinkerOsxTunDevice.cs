using linker.libs;
using linker.libs.extends;
using Microsoft.Win32.SafeHandles;
using System.Buffers.Binary;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace linker.tun.device
{
    /// <summary>
    /// osx网卡实现，未测试
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
                // macOS'ta utun cihazları otomatik olarak unit numarası ile oluşturulur
                // -1 kullanarak sistemin otomatik olarak bir unit atamasını sağlıyoruz
                IntPtr ifnameBuffer = Marshal.AllocHGlobal(256);


                try
                {
                    int fd = MacAPI.open_utun(-1, ifnameBuffer, new UIntPtr(256), out int errno);


                    if (fd < 0)
                    {
                        error = $"Failed to open utun device. Error: {errno}";
                        return false;
                    }


                    // Interface adını al
                    interfaceMac = Marshal.PtrToStringAnsi(ifnameBuffer);
                    if (string.IsNullOrEmpty(interfaceMac))
                    {
                        error = "Failed to get interface name";
                        return false;
                    }


                    // Unit numarasını çıkar (örn: utun5 -> 5)
                    var match = Regex.Match(interfaceMac, @"utun(\d+)");
                    if (match.Success)
                    {
                        tunUnit = int.Parse(match.Groups[1].Value);
                    }


                    // SafeFileHandle oluştur
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
                // macOS'ta TUN interface için gateway IP (genellikle .1)
                byte[] gatewayBytes = address.GetAddressBytes();
                gatewayBytes[3] = 1; // Son octet'i 1 yap (örn: 10.18.18.1)
                IPAddress gatewayAddr = new IPAddress(gatewayBytes);


                IPAddress networkAddr = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));


                string[] commands = new string[]
                {
                    // Interface'i configure et - destination olarak gateway kullan
                    $"sudo ifconfig {interfaceMac} {address} {gatewayAddr} netmask 255.255.255.255 up",
                    $"sudo ifconfig {interfaceMac} mtu 1500",
                    
                    // IP forwarding'i aktifleştir
                    "sudo sysctl -w net.inet.ip.forwarding=1",
                    "sudo sysctl -w net.inet.ip.redirect=0",
                    
                    // Eski route'ları temizle
                    $"sudo route delete -net {networkAddr}/{prefixLength} 2>/dev/null || true",
                    
                    // Network route'u ekle - interface üzerinden
                    $"sudo route add -net {networkAddr}/{prefixLength} -interface {interfaceMac}",
                    
                    // Host route ekle - kendisi için
                    $"sudo route add -host {address} -interface {interfaceMac}",
                    
                    // Gateway route ekle
                    $"sudo route add -host {gatewayAddr} -interface {interfaceMac}"
                };


                string result = CommandHelper.Osx(string.Empty, commands, out error);


                // Route ekleme hatası normal olabilir (zaten varsa)
                if (!string.IsNullOrEmpty(error))
                {
                    // Route hatalarını ignore et ama diğer hatalar için kontrol et
                    if (!(!error.Contains("File exists") && !error.Contains("Network is unreachable") && !error.Contains("route: writing to routing socket")))
                    {
                        // Kritik olmayan hatalar için devam et
                        error = string.Empty;
                    }
                }


                // Interface'in UP olduğunu kontrol et
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
                    // Interface'i down yap
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
                    // Fallback - en0 veya eth0 dene
                    defaultInterface = "en0";
                }


                // Önce mevcut pfctl durumunu kontrol et
                string pfStatus = CommandHelper.Osx(string.Empty, new string[] { "sudo pfctl -s info" });


                // Basit NAT kuralları
                string pfRules = $@"# Localtonet VPN NAT Rules
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


                // pfctl konfigürasyonu
                string tempFile = "/tmp/localtonet_pf_rules";
                File.WriteAllText(tempFile, pfRules);


                // IP forwarding'i aktifleştir
                CommandHelper.Osx(string.Empty, new string[] {
                    "sudo sysctl -w net.inet.ip.forwarding=1"
                });


                // pfctl kurallarını yükle
                string pfResult = CommandHelper.Osx(string.Empty, new string[] {
                    $"sudo pfctl -f {tempFile}",
                    "sudo pfctl -e"
                }, out error);


                // Geçici dosyayı sil
                try { File.Delete(tempFile); } catch { }





                // pfctl durumunu kontrol et
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
                // pfctl'i disable et
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
            // macOS'ta port forwarding genellikle pfctl ile yapılır
            // Basit bir implementasyon - gerçek kullanımda daha karmaşık parsing gerekebilir
            var forwards = new List<LinkerTunDeviceForwardItem>();


            try
            {
                string result = CommandHelper.Osx(string.Empty, new string[] { "sudo pfctl -s nat" });
                // pfctl output'unu parse etmek için regex kullan
                // Bu basit bir örnek - gerçek implementasyon daha karmaşık olabilir
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
                    string tempFile = "/tmp/localtonet_forward_rules";
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
            // pfctl rules'ları kaldırmak için genellikle tüm konfigürasyon yeniden yüklenir
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


        public byte[] Read(out int length)
        {
            length = 0;
            if (safeFileHandle == null || fsRead == null) return Helper.EmptyArray;


            // UTUN: tek okuyuşta [AF(4) | IP(...)]
            int n = fsRead.Read(buffer, 0, buffer.Length);
            if (n < 5) return Helper.EmptyArray;


            // AF header BIG-ENDIAN
            uint af = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(0, 4));
            if (af != 2u && af != 30u)  // AF_INET=2, AF_INET6=30
                return Helper.EmptyArray;


            int payloadLen = n - 4;


            // Senin pipeline: [LEN_LE(4) | IP]
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), payloadLen);
            Buffer.BlockCopy(buffer, 4, buffer, 4, payloadLen);


            length = payloadLen + 4;
            return buffer;
        }



        public bool Write(ReadOnlyMemory<byte> packet)
        {
            if (safeFileHandle == null || fsWrite == null) return false;


            lock (writeLockObj)
            {
                try
                {
                    var span = packet.Span;
                    if (span.Length < 1) return false;


                    ReadOnlySpan<byte> ipSpan;


                    // 1) UTUN çerçevesi mi? (AF header big-endian: 0x00000002 veya 0x0000001E)
                    if (span.Length >= 5)
                    {
                        uint afBe = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(0, 4));
                        if (afBe == 2u || afBe == 30u)
                        {
                            // Zaten [AF_BE][IP] -> direkt yaz
                            fsWrite.Write(span);
                            // fsWrite.Flush(); // genelde gerek yok
                            return true;
                        }
                    }


                    // 2) Ham IP mi? (ilk nibble 4 ya da 6)
                    byte v = (byte)(span[0] >> 4);
                    if (v == 4 || v == 6)
                    {
                        ipSpan = span; // [IP]
                    }
                    else
                    {
                        // 3) [LEN_LE][IP] çerçevesi
                        if (span.Length < 5) return false;
                        int payloadLen = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(0, 4));
                        // Mantık kontrolü
                        if (payloadLen <= 0 || payloadLen > span.Length - 4) return false;


                        ipSpan = span.Slice(4, payloadLen);


                        // Güvenlik: gerçekten IP mi?
                        byte v2 = (byte)(ipSpan[0] >> 4);
                        if (v2 != 4 && v2 != 6) return false;
                        v = v2;
                    }


                    uint af = (v == 6) ? 30u : 2u; // AF_INET6 / AF_INET


                    // UTUN paketi oluştur: [AF_BE(4)] + [IP]
                    byte[] outBuf = new byte[4 + ipSpan.Length];
                    BinaryPrimitives.WriteUInt32BigEndian(outBuf.AsSpan(0, 4), af);
                    ipSpan.CopyTo(outBuf.AsSpan(4));


                    fsWrite.Write(outBuf, 0, outBuf.Length);
                    fsWrite.Flush(); // çoğu zaman gerekmez
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }


        public async Task<bool> CheckAvailable(bool order = false)
        {
            if (string.IsNullOrEmpty(interfaceMac))
                return await Task.FromResult(false);


            try
            {
                string output = CommandHelper.Osx(string.Empty, new string[] { $"ifconfig {interfaceMac}" });
                return await Task.FromResult(output.Contains("UP") && output.Contains(address.ToString()));
            }
            catch (Exception)
            {
                return await Task.FromResult(false);
            }
        }
    }


    // Mac API için gerekli P/Invoke tanımları
    internal static class MacAPI
    {
        [DllImport("libutunshim.dylib", CallingConvention = CallingConvention.Cdecl)]
        public static extern int open_utun(int unit, IntPtr ifnameBuf, UIntPtr ifnameLen, out int out_errno);
    }


}
