using linker.libs;
using linker.nat;
using linker.tun.device;
using System.Net;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanDstProxy : ILinkerTunPacketHook
    {
        public LinkerTunPacketHookLevel Level => LinkerTunPacketHookLevel.Highest;
        private LinkerDstProxy linkerDstNat = new LinkerDstProxy();

        public bool Running => linkerDstNat.Running;

        public LinkerTunPacketHookLanDstProxy()
        {
        }

        public void Setup(IPAddress address, byte prefixLength, LinkerTunAppNatItemInfo[] items, ref string error)
        {
            if (OperatingSystem.IsWindows() == false) return;

            if (address == null || address.Equals(IPAddress.Any) || prefixLength == 0)
            {
                error = "DstProxy need CIDR,like 10.18.18.0/24";
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

            linkerDstNat.Setup(address, items.Select(c => new ValueTuple<IPAddress, byte>(c.IP, c.PrefixLength)).ToArray(), ref error);
        }
        public void Shutdown()
        {
            try
            {
                linkerDstNat.Shutdown();
            }
            catch (Exception)
            {
            }
            GC.Collect();
        }

        public bool Read(ReadOnlyMemory<byte> packet)
        {
            linkerDstNat.Read(packet);
            return true;
        }
        public bool Write(string srcId, ReadOnlyMemory<byte> packet)
        {
            return linkerDstNat.Write(packet);
        }
    }
}
