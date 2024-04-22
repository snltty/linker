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
            MemoryPackIPEndPointSerializeInfo info = new MemoryPackIPEndPointSerializeInfo { EP = new IPEndPoint(IPAddress.Loopback, 12345) };
            MemoryPackIPEndPointSerializeInfo info1 = MemoryPackSerializer.Deserialize<MemoryPackIPEndPointSerializeInfo>(MemoryPackSerializer.Serialize(info));

            Assert.AreEqual(info.EP, info1.EP);

        }
    }

    [MemoryPackable]
    public sealed partial class MemoryPackIPEndPointSerializeInfo
    {
        [MemoryPackAllowSerialize]
        public IPEndPoint EP { get; set; }
    }
}