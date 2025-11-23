
using linker.messenger.socks5;
using LiteDB;
using System.Text.Json.Serialization;

namespace linker.messenger.store.file
{
    public sealed partial class Socks5ConfigInfo
    {
        public Socks5ConfigInfo() { }
        public int Port { get; set; } = 1805;
        /// <summary>
        /// 局域网配置列表
        /// </summary>
        public List<Socks5LanInfo> Lans { get; set; } = new List<Socks5LanInfo>();

        /// <summary>
        /// 是否在运行中
        /// </summary>
        public bool Running { get; set; }
    }
    public sealed partial class RunningConfigInfo
    {
        public Socks5ConfigInfo Socks5 { get; set; } = new Socks5ConfigInfo();
    }
}
