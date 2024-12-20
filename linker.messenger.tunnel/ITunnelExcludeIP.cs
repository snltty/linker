using System.Net;

namespace linker.messenger.tunnel
{
    /// <summary>
    /// 打洞排除IP
    /// </summary>
    public interface ITunnelExcludeIP
    {
        public ExcludeIPItem[] Get();
    }

    /// <summary>
    /// 排除的IP
    /// </summary>
    public sealed partial class ExcludeIPItem
    {
        public ExcludeIPItem() { }
        public IPAddress IPAddress { get; set; }
        public byte Mask { get; set; } = 32;
    }
}
