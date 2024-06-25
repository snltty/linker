using System.Net;

namespace linker.tunnel.wanport
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public interface ITunnelWanPort
    {
        public string Name { get; }
        public TunnelWanPortType Type { get; }
        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="server">服务端</param>
        /// <returns></returns>
        public Task<TunnelWanPortEndPoint> GetAsync(IPEndPoint server);
    }

    public sealed class TunnelWanPortEndPoint
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

    public sealed partial class TunnelWanPortInfo
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 协议类别
        /// </summary>
        public TunnelWanPortType Type { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public string Host { get; set; } = string.Empty;
        /// <summary>
        /// 是否禁用
        /// </summary>
        public bool Disabled { get; set; }
    }

    public enum TunnelWanPortType : byte
    {
        Linker = 0,
        Stun = 1
    }

    public sealed class TunnelWanPortTypeInfo
    {
        public TunnelWanPortType Value { get; set; }
        public string Name { get; set; }
    }

    public sealed class TunnelWanPortTypeInfoEqualityComparer : IEqualityComparer<TunnelWanPortTypeInfo>
    {
        public bool Equals(TunnelWanPortTypeInfo x, TunnelWanPortTypeInfo y)
        {
            return x.Value == y.Value;
        }

        public int GetHashCode(TunnelWanPortTypeInfo obj)
        {
            return obj.Value.GetHashCode();
        }
    }


}
