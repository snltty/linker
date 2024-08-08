
using linker.config;
using linker.plugins.serializes;
using linker.plugins.signin.messenger;
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




            byte[] bytes = MemoryPackSerializer.Serialize(new SignInListRequestInfo1
            {
                GroupId = string.Empty,
                Ids =[],
                Name = "11",
                Page = 1,
                Size = 1,
            });

            SignInListRequestInfo tuntapInfos1 = MemoryPackSerializer.Deserialize<SignInListRequestInfo>(bytes);

            Assert.AreEqual(tuntapInfos1.Name, "11");
        }
    }

    [MemoryPackable]
    public sealed partial class SignInListRequestInfo1
    {
        /// <summary>
        /// 当前页
        /// </summary>
        public int Page { get; set; } = 1;
        /// <summary>
        /// 每页大小
        /// </summary>
        public int Size { get; set; } = 10;
        /// <summary>
        /// 所在分组
        /// </summary>
        public string GroupId { get; set; }
        /// <summary>
        /// 按名称搜索
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 按id获取
        /// </summary>
        public string[] Ids { get; set; }
    }


}