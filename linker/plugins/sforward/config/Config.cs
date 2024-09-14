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

        /// <summary>
        /// 端口范围
        /// </summary>
        public int RemotePortMin { get; set; }
        /// <summary>
        /// 端口范围
        /// </summary>
        public int RemotePortMax { get; set; }
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
    /// <summary>
    /// 往服务器添加穿透
    /// </summary>
    [MemoryPackable]
    public sealed partial class SForwardAddInfo
    {
        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 或者端口。域名优先
        /// </summary>
        public int RemotePort { get; set; }
        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; }
    }

    /// <summary>
    /// 添加穿透结果
    /// </summary>
    [MemoryPackable]
    public sealed partial class SForwardAddResultInfo
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 失败信息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// buffsize
        /// </summary>
        public byte BufferSize { get; set; }
    }

    /// <summary>
    /// 服务器穿透代理信息
    /// </summary>
    [MemoryPackable]
    public sealed partial class SForwardProxyInfo
    {
        /// <summary>
        /// 请求编号
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 端口
        /// </summary>
        public int RemotePort { get; set; }
        /// <summary>
        /// bufsize
        /// </summary>
        public byte BufferSize { get; set; } = 3;
    }
}
