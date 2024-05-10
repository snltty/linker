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
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());
            TunnelTransportInfo info = new TunnelTransportInfo
            {
                Direction = client.tunnel.TunnelDirection.Forward,
                Local = new TunnelTransportExternalIPInfo
                {
                    Local = new IPEndPoint(IPAddress.Loopback, 12345),
                    Remote = new IPEndPoint(IPAddress.Loopback, 12345),
                    MachineName = "111",
                    RouteLevel = 1,
                    LocalIps = new IPAddress[] { IPAddress.Loopback }
                },
                Remote = new TunnelTransportExternalIPInfo
                {
                    Local = new IPEndPoint(IPAddress.Loopback, 12345),
                    Remote = new IPEndPoint(IPAddress.Loopback, 12345),
                    MachineName = "111",
                    RouteLevel = 1,
                    LocalIps = new IPAddress[] { IPAddress.Loopback }
                },
                TransactionId = "111",
                TransportName = "111",
                TransportType = client.tunnel.TunnelProtocolType.Tcp
            };
            TunnelTransportInfo info1 = MemoryPackSerializer.Deserialize<TunnelTransportInfo>(MemoryPackSerializer.Serialize(info));

            Assert.AreEqual(info.Local.Local, info1.Local.Local);
            Assert.AreEqual(info.Local.LocalIps[0], info1.Local.LocalIps[0]);

        }
    }

    [MemoryPackable]
    public sealed partial class MemoryPackIPEndPointSerializeInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint EP { get; set; }
    }
}