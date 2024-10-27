using LiteDB;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;

namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 端口转发配置
        /// </summary>
        public List<ForwardInfo> Forwards { get; set; } = new List<ForwardInfo>();
    }

    /// <summary>
    /// 端口转发配置
    /// </summary>
    [MemoryPackable]
    public sealed partial class ForwardInfo
    {
        public ForwardInfo() { }
        public uint Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 目标设备
        /// </summary>
        public string MachineId { get; set; }
        public string MachineName { get; set; }
        /// <summary>
        /// 本地绑定IP
        /// </summary>
        [MemoryPackAllowSerialize]
        public IPAddress BindIPAddress { get; set; } = IPAddress.Any;
        /// <summary>
        /// 本地监听端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 目标设备服务
        /// </summary>
        [MemoryPackAllowSerialize]
        public IPEndPoint TargetEP { get; set; }
        /// <summary>
        /// 已启动
        /// </summary>
        public bool Started { get; set; }
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;

        /// <summary>
        /// 本地监听错误信息
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 目标服务错误信息
        /// </summary>
        public string TargetMsg { get; set; }

        [JsonIgnore, BsonIgnore,MemoryPackIgnore]
        public bool Proxy { get; set; }

    }
}
