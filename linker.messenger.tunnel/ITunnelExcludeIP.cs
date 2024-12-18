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

    public sealed partial class ExcludeIPItem
    {
        public ExcludeIPItem() { }
        public IPAddress IPAddress { get; set; }
        public byte Mask { get; set; } = 32;
    }
}
