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

        public bool Read(ReadOnlyMemory<byte> packet, ref bool send, ref bool writeBack)
        {
            LinkerSrcProxy.Read(packet, ref send, ref writeBack);
            return send;
        }
        public async ValueTask<(bool next, bool write)> WriteAsync(ReadOnlyMemory<byte> packet, uint originDstIp, string srcId)
        {
            bool write = await LinkerSrcProxy.WriteAsync(packet,originDstIp).ConfigureAwait(false);
            return await ValueTask.FromResult((write, write));
        }
    }
}
