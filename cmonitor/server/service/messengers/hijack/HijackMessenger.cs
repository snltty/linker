using cmonitor.hijack;
using MemoryPack;

namespace cmonitor.server.service.messengers.hijack
{
    public sealed class HijackMessenger : IMessenger
    {
        private readonly HijackConfig hijackConfig;
        private readonly HijackController hijackController;

        public HijackMessenger(HijackConfig hijackConfig, HijackController hijackController)
        {
            this.hijackConfig = hijackConfig;
            this.hijackController = hijackController;
        }

        [MessengerId((ushort)HijackMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            SetRuleInfo info = MemoryPackSerializer.Deserialize<SetRuleInfo>(connection.ReceiveRequestWrap.Payload.Span);

            hijackConfig.AllowDomains = info.AllowDomains;
            hijackConfig.DeniedDomains = info.DeniedDomains;
            hijackConfig.AllowProcesss = info.AllowProcesss;
            hijackConfig.DeniedProcesss = info.DeniedProcesss;
            hijackConfig.AllowIPs = info.AllowIPs;
            hijackConfig.DeniedIPs = info.DeniedIPs;
            hijackController.SetRules();
        }

    }

    [MemoryPackable]
    public sealed partial class SetRuleInfo
    {
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
