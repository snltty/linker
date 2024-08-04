
using linker.config;
using linker.plugins.serializes;
using linker.plugins.tuntap.config;
using linker.tunnel.connection;
using linker.tunnel.transport;
using MemoryPack;
using System.Net;

namespace linker.Tests
{
    [TestClass]
    public class MemoryPackIPEndPointSerialize
    {
        [TestMethod]
        public void Serialize()
        {
            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());


            TuntapInfo tuntapInfo = new TuntapInfo
            {
                Error = "dfgdgdfgdfgddfgdfhdhdhdhdfhdfdfffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                Gateway = false,
                IP = IPAddress.Any,
                LanIPs = new IPAddress[] { IPAddress.Any, IPAddress.Loopback, IPAddress.Broadcast },
                Masks = [24, 24, 24],
                MachineId = "dfgdgdfgdfgddfgdfhdhdhdhdfhdfdfffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                Status = TuntapStatus.Normal,
                System = "dfgdgdfgdfgddfgdfhdhdhdhdfhdfdfffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
            };
            List<TuntapInfo> tuntapInfos = new List<TuntapInfo>();
            for (int i = 0; i < 100; i++)
            {
                tuntapInfos.Add(tuntapInfo);
            }

            byte[] bytes = MemoryPackSerializer.Serialize(tuntapInfos);

            List<TuntapInfo> tuntapInfos1 = MemoryPackSerializer.Deserialize<List<TuntapInfo>>(bytes);

            Assert.AreEqual(tuntapInfos1.Count, tuntapInfos.Count);
        }
    }


}