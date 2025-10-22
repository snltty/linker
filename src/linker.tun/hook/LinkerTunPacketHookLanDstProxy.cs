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
            if (OperatingSystem.IsWindows() == false) return;

            if (address == null || address.Equals(IPAddress.Any) || prefixLength == 0)
            {
                error = "DstProxy need CIDR,like 10.18.18.0/24";
                return;
            }

            try
            {
                IPAddress network = NetworkHelper.ToNetworkIP(address, NetworkHelper.ToPrefixValue(prefixLength));
                string result = OperatingSystem.IsWindows()
                    ? CommandHelper.PowerShell($"Get-NetNat", [], out string e)
                    : OperatingSystem.IsLinux() ? CommandHelper.Linux(string.Empty, new string[] { $"iptables -t nat -L --line-numbers" }) : string.Empty;
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

        public (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet)
        {
            linkerDstProxy.Read(packet);
            return (LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None);
        }
        public ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            if (linkerDstProxy.Write(packet))
            {
                return ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
            }
            return ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.Next | LinkerTunPacketHookFlags.Write));
        }
    }
}
