using linker.libs;
using linker.snat;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace linker.tun
{
    internal sealed class LanSnat : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Highest;
        public bool Running => linkerSrcNat.Running;

        private LinkerSrcNat linkerSrcNat = new LinkerSrcNat();
       

        public LanSnat()
        {
        }

        private void Helper_OnAppExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        public void Setup(IPAddress address, byte prefixLength, LinkerTunAppNatItemInfo[] items, ref string error)
        {
            if (OperatingSystem.IsWindows() == false) return;

            Shutdown();

            if (address == null || address.Equals(IPAddress.Any) || prefixLength == 0)
            {
                error = "SNAT need CIDR,like 10.18.18.0/24";
                return;
            }
            IPAddress defaultInterfaceIP = GetDefaultInterface();
            if (defaultInterfaceIP == null)
            {
                error = "SNAT get default interface id fail";
                return;
            }

            IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));
            string result = CommandHelper.PowerShell($"Get-NetNat", [], out string e);
            if (string.IsNullOrWhiteSpace(result) == false && result.Contains($"{network}/{prefixLength}"))
            {
                return;
            }
            linkerSrcNat.Setup(new LinkerSrcNat.SetupInfo
            {
                Src = address,
                Dsts = items.Select(c => new LinkerSrcNat.AddrInfo(c.IP, c.PrefixLength)).ToArray(),
                InterfaceIp = defaultInterfaceIP
            }, out error);
        }
        public void Shutdown()
        {
            try
            {
                linkerSrcNat.Shutdown();
            }
            catch (Exception)
            {
            }
        }

        public bool ReadAfter(ReadOnlyMemory<byte> packet)
        {
            return false;
        }
        public bool WriteBefore(string srcId, ReadOnlyMemory<byte> packet)
        {
            if (linkerSrcNat.Running == false) return true;

            return linkerSrcNat.Inject(packet) == false;
        }


        private IPAddress GetDefaultInterface()
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
                                return ip;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            return null;
        }
    }
}
