using linker.libs;
using linker.nat;
using linker.tun.device;
using System.Net;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanSrcNat : ILinkerTunPacketHook
    {
        public string Name => "DstNat";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Lowest;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.Highest;
        public bool Running => linkerSrcNat.Running;

        private LinkerSrcNat linkerSrcNat = new LinkerSrcNat();


        public LinkerTunPacketHookLanSrcNat()
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

        public (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet)
        {
            return (LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None);
        }
        public ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            return ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
        }
    }
}
