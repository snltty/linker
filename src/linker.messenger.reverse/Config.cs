using System.Net;
using System.Net.Sockets;


namespace linker.messenger.reverse
{
    public partial class ReverseInfo
    {
        public ReverseInfo() { }
        /// <summary>
        /// 穿透id
        /// </summary>
        public long Id { get; set; }
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

        /// <summary>
        /// 端口范围
        /// </summary>
        public int RemotePortMin { get; set; }
        /// <summary>
        /// 端口范围
        /// </summary>
        public int RemotePortMax { get; set; }

        /// <summary>
        /// 191+
        /// </summary>
        public string NodeId { get; set; } = string.Empty;
        public string NodeId1 { get; set; } = string.Empty;
    }

   
    /// <summary>
    /// 往服务器添加穿透
    /// </summary>
    public partial class ReverseAddInfo
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
        /// 191+
        /// </summary>
        public string NodeId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;

        public bool Super { get; set; }
        public double Bandwidth { get; set; }
    }
    /// <summary>
    /// 添加穿透结果
    /// </summary>
    public sealed partial class ReverseAddResultInfo
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

    public partial class ReverseAddForwardInfo
    {
        public string MachineId { get; set; }
        public ReverseInfo Data { get; set; }
    }
    public sealed partial class ReverseRemoveForwardInfo
    {
        public string MachineId { get; set; }
        public int Id { get; set; }
    }

    /// <summary>
    /// 服务器穿透代理信息
    /// </summary>
    public partial class ReverseProxyInfo
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

        public string MachineId { get; set; }
        public ProtocolType ProtocolType { get; set; }
        public string NodeId { get; set; }
        public IPAddress Addr { get; set; }
    }

}
