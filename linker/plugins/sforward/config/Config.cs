using LiteDB;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 服务器穿透密钥
        /// </summary>
        public string SForwardSecretKey { get; set; } = "snltty";
        /// <summary>
        /// 服务器穿透列表
        /// </summary>
        public List<SForwardInfo> SForwards { get; set; } =new List<SForwardInfo>();
    }

    public sealed class SForwardInfo
    {
        /// <summary>
        /// 穿透id
        /// </summary>
        public uint Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 端口，
        /// </summary>
        public int RemotePort { get; set; }
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;
        /// <summary>
        /// 本地服务
        /// </summary>
        public IPEndPoint LocalEP { get; set; }
        /// <summary>
        /// 已启动
        /// </summary>
        public bool Started { get; set; }
        /// <summary>
        /// 服务器错误信息
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 本地错误信息
        /// </summary>
        public string LocalMsg { get; set; }

        [JsonIgnore, BsonIgnore]
        public bool Proxy { get; set; }

        public int RemotePortMin { get; set; }
        public int RemotePortMax { get; set; }
    }


    public sealed class SForwardSetStatusInfo
    {

    }
}

namespace linker.config
{
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 服务器穿透配置
        /// </summary>
        public SForwardConfigServerInfo SForward { get; set; } = new SForwardConfigServerInfo();
    }

    public sealed class SForwardConfigServerInfo
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;
        /// <summary>
        /// web端口
        /// </summary>
        public int WebPort { get; set; }
        /// <summary>
        /// 开放端口范围
        /// </summary>
        public int[] TunnelPortRange { get; set; } = new int[] { 10000, 60000 };

    }

}

namespace linker.plugins.sforward.config
{
    [MemoryPackable]
    public sealed partial class SForwardAddInfo
    {
        public string Domain { get; set; }
        public int RemotePort { get; set; }
        public string SecretKey { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SForwardAddResultInfo
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public byte BufferSize { get; set; }
    }


    [MemoryPackable]
    public sealed partial class SForwardProxyInfo
    {
        public ulong Id { get; set; }
        public string Domain { get; set; }
        public int RemotePort { get; set; }
        public byte BufferSize { get; set; } = 3;
    }
}
