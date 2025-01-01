using linker.libs;
using linker.libs.extends;
using LiteDB;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace linker.messenger.store.file
{

    public sealed partial class ConfigCommonInfo
    {
#if DEBUG
        private LoggerTypes loggerType { get; set; } = LoggerTypes.DEBUG;
#else
        private LoggerTypes loggerType { get; set; } = LoggerTypes.WARNING;
#endif

        public LoggerTypes LoggerType
        {
            get => loggerType; set
            {
                loggerType = value;
                LoggerHelper.Instance.LoggerLevel = value;
            }
        }
        public int LoggerSize { get; set; } = 100;
    }
}
