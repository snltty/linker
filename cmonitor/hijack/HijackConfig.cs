namespace cmonitor.hijack
{
    public sealed class HijackConfig
    {
        public HijackConfig() { }

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

    }

}
