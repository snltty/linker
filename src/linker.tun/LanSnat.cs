using linker.libs;
using linker.snat;
using System.Net;

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

            if (address == null || address.Equals(IPAddress.Any) || prefixLength == 0)
            {
                error = "SNAT need CIDR,like 10.18.18.0/24";
                return;
            }

            try
            {
                IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));
                string result = CommandHelper.PowerShell($"Get-NetNat", [], out string e);
                if (string.IsNullOrWhiteSpace(result) == false && result.Contains($"{network}/{prefixLength}"))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }

            Shutdown();
            linkerSrcNat.Setup(new LinkerSrcNat.SetupInfo
            {
                Src = address,
                Dsts = items.Select(c => new LinkerSrcNat.AddrInfo(c.IP, c.PrefixLength)).ToArray()
            }, ref error);
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
            GC.Collect();
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
    }
}
