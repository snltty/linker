using linker.nat;
using System.Net;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanSrcProxy : ILinkerTunPacketHook
    {
        public string Name => "SrcProxy";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Low9;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.High9;
        private readonly LinkerSrcProxy LinkerSrcProxy = new LinkerSrcProxy();

        public bool Running => LinkerSrcProxy.Running;

        public LinkerTunPacketHookLanSrcProxy()
        {
        }

        public void Setup(IPAddress address, byte prefixLength, ILinkerSrcProxyCallback callback, ref string error)
        {
            LinkerSrcProxy.Setup(address, prefixLength, callback, ref error);
        }
        public void Shutdown()
        {
            try
            {
                LinkerSrcProxy.Shutdown();
            }
            catch (Exception)
            {
            }
            GC.Collect();
        }

        public (LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del) Read(ReadOnlyMemory<byte> packet)
        {
            if (LinkerSrcProxy.Read(packet))
            {
                return (LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None);
            }
            return (LinkerTunPacketHookFlags.WriteBack, LinkerTunPacketHookFlags.Next | LinkerTunPacketHookFlags.Send);
        }
        public async ValueTask<(LinkerTunPacketHookFlags add, LinkerTunPacketHookFlags del)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            if (await LinkerSrcProxy.WriteAsync(packet, originDstIp).ConfigureAwait(false))
            {
                return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.None));
            }
            return await ValueTask.FromResult((LinkerTunPacketHookFlags.None, LinkerTunPacketHookFlags.Next | LinkerTunPacketHookFlags.Write));
        }
    }
}
