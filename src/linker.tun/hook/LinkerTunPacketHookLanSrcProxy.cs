using linker.nat;
using System.Net;

namespace linker.tun.hook
{
    internal sealed class LinkerTunPacketHookLanSrcProxy : ILinkerTunPacketHook
    {
        public string Name => "SrcProxy";
        public LinkerTunPacketHookLevel ReadLevel => LinkerTunPacketHookLevel.Low9;
        public LinkerTunPacketHookLevel WriteLevel => LinkerTunPacketHookLevel.Normal;
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
        public bool Write(ReadOnlyMemory<byte> packet, string srcId, ref bool write)
        {
            LinkerSrcProxy.Write(packet, ref write);
            return write;
        }
    }
}
