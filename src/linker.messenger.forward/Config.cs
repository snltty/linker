using System.Net;
namespace linker.messenger.forward
{
    /// <summary>
    /// 端口转发配置
    /// </summary>
    public sealed partial class ForwardInfo
    {
        public ForwardInfo() { }
        public long Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 目标设备
        /// </summary>
        public string MachineId { get; set; }
        public string GroupId { get; set; }
        public string MachineName { get; set; }
        /// <summary>
        /// 本地绑定IP
        /// </summary>
        public IPAddress BindIPAddress { get; set; } = IPAddress.Any;
        /// <summary>
        /// 本地监听端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 目标设备服务
        /// </summary>
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

        public bool Proxy { get; set; }

    }

    public sealed partial class ForwardAddForwardInfo
    {
        public string MachineId { get; set; }
        public ForwardInfo Data { get; set; }
    }
    public sealed partial class ForwardRemoveForwardInfo
    {
        public string MachineId { get; set; }
        public int Id { get; set; }
    }
    public sealed partial class ForwardCountInfo
    {
        public string MachineId { get; set; }
        public int Count { get; set; }
    }
    public sealed partial class ForwardTestInfo
    {
        public IPEndPoint Target { get; set; }
        public string Msg { get; set; } = string.Empty;
    }
}
