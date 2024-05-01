using cmonitor.plugins.tunnel.server;
using cmonitor.plugins.tunnel.transport;
using cmonitor.serializes;
using MemoryPack;
using System.Net;

namespace cmonitor.tests
{
    [TestClass]
    public class MemoryPackIPEndPointSerialize
    {
        [TestMethod]
        public void Serialize()
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            TunnelTransportInfo info = new TunnelTransportInfo
            {
                Direction = TunnelTransportDirection.Reverse,
                Local = new TunnelTransportExternalIPInfo { Local = new IPEndPoint(IPAddress.Loopback, 12345), Remote = new IPEndPoint(IPAddress.Loopback, 12345), MachineName = "111", RouteLevel = 1 },
                Remote = new TunnelTransportExternalIPInfo { Local = new IPEndPoint(IPAddress.Loopback, 12345), Remote = new IPEndPoint(IPAddress.Loopback, 12345), MachineName = "111", RouteLevel = 1 },
                TransactionId = "111",
                TransportName = "111",
                TransportType = System.Net.Sockets.ProtocolType.Tcp
            };
            TunnelTransportInfo info1 = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(MemoryPackSerializer.Serialize(info));

            Assert.AreEqual(info.Local.Local, info1.Local.Local);

        }
    }

    [MemoryPackable]
    public sealed partial class MemoryPackIPEndPointSerializeInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint EP { get; set; }
    }
}