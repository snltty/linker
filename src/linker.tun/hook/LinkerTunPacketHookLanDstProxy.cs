using linker.libs;
using linker.nat;
using linker.tun.device;
using System.Net;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanDstProxy : ILinkerTunPacketHook
    {
        public string Name => "DstProxy";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Lowest;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.Highest;

        private readonly LinkerDstProxy linkerDstProxy = new LinkerDstProxy();

        public bool Running => linkerDstProxy.Running;

        public LinkerTunPacketHookLanDstProxy()
        {
        }

        public void Setup(IPAddress address, byte prefixLength, LinkerTunAppNatItemInfo[] items, ref string error)
        {
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

            linkerDstProxy.Setup(address, items.Select(c => new ValueTuple<IPAddress, byte>(c.IP, c.PrefixLength)).ToArray(), ref error);
        }
        public void Shutdown()
        {
            try
            {
                linkerDstProxy.Shutdown();
            }
            catch (Exception)
            {
            }
            GC.Collect();
        }

        public bool Read(ReadOnlyMemory<byte> packet, ref bool send, ref bool writeBack)
        {
            linkerDstProxy.Read(packet);
            return true;
        }
        public ValueTask<(bool next, bool write)> WriteAsync(ReadOnlyMemory<byte> packet, string srcId)
        {
            return ValueTask.FromResult((true, linkerDstProxy.Write(packet)));
        }
    }
}
