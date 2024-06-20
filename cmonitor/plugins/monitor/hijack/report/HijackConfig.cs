using cmonitor.plugins.hijack.report;
using LiteDB;

namespace cmonitor.plugins.hijack.report
{
    public sealed class HijackConfig
    {
        public HijackConfig()
        {

        }
        public ObjectId Id { get; set; }

        /// <summary>
        /// 进程白名单
        /// </summary>
        public string[] AllowProcesss { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 进程黑名单
        /// </summary>
        public string[] DeniedProcesss { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 域名白名单
        /// </summary>
        public string[] AllowDomains { get; set; } = Array.Empty<string>();
        /// <summary>
        /// 域名黑名单
        /// </summary>
        public string[] DeniedDomains { get; set; } = Array.Empty<string>();

        /// <summary>
        /// ip白名单
        /// </summary>
        public string[] AllowIPs { get; set; } = Array.Empty<string>();
        /// <summary>
        /// ip黑名单
        /// </summary>
        public string[] DeniedIPs { get; set; } = Array.Empty<string>();

        public string[] HijackIds1 { get; set; } = Array.Empty<string>();
        public string[] HijackIds2 { get; set; } = Array.Empty<string>();

        public bool DomainKill { get; set; }

    }

}

namespace cmonitor.client.config
{
    public sealed partial class RunningConfigInfo
    {
        private HijackConfig hijack = new HijackConfig();
        public HijackConfig Hijack
        {
            get => hijack; set
            {
                Updated++;
                hijack = value;
            }
        }
    }
}
