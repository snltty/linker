using System.Net;

namespace linker.tunnel.compact
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public interface ITunnelCompact
    {
        public string Name { get; }
        public TunnelCompactType Type { get; }
        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="server">服务端</param>
        /// <returns></returns>
        public Task<TunnelCompactIPEndPoint> GetExternalIPAsync(IPEndPoint server);
    }

    public sealed class TunnelCompactIPEndPoint
    {
        /// <summary>
        /// 内网
        /// </summary>
        public IPEndPoint Local { get; set; }
        /// <summary>
        /// 外网
        /// </summary>
        public IPEndPoint Remote { get; set; }
    }

    public sealed partial class TunnelCompactInfo
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 协议类别
        /// </summary>
        public TunnelCompactType Type { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public string Host { get; set; } = string.Empty;
        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool Disabled { get; set; }
    }

    public enum TunnelCompactType : byte
    {
        Link = 0,
        Stun = 1
    }

    public sealed class TunnelCompactTypeInfo
    {
        public TunnelCompactType Value { get; set; }
        public string Name { get; set; }
    }

    public sealed class TunnelCompactTypeInfoEqualityComparer : IEqualityComparer<TunnelCompactTypeInfo>
    {
        public bool Equals(TunnelCompactTypeInfo x, TunnelCompactTypeInfo y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode(TunnelCompactTypeInfo obj)
        {
            return obj.Value.GetHashCode();
        }
    }


}
