using common.libs.extends;
using common.libs.api;
using common.libs;
using cmonitor.config;
using cmonitor.plugins.capi;

namespace cmonitor.plugins.logger
{
    public sealed class LoggerClientApiController : IApiClientController
    {
        private readonly List<LoggerModel> loggers = new List<LoggerModel>();

        private readonly Config config;
        public LoggerClientApiController(Config config)
        {
            this.config = config;
            Logger.Instance.OnLogger += (LoggerModel logger) =>
            {
                loggers.Add(logger);
                if (loggers.Count > config.Data.Common.LoggerSize)
                {
                    loggers.RemoveAt(0);
                }
            };
        }

        public LoggerPageInfo Get(ApiControllerParamsInfo param)
        {
            LoggerPageParamInfo info = param.Content.DeJson<LoggerPageParamInfo>();

            IEnumerable<LoggerModel> result = loggers;
            if (info.Type >= 0)
            {
                result = result.Where(c => c.Type == (LoggerTypes)info.Type);
            }
            result = result.OrderByDescending(c => c.Time);

            return new LoggerPageInfo
            {
                Page = info.Page,
                Size = info.Size,
                Type = info.Type,
                Count = result.Count(),
                List = result.Skip((info.Page - 1) * info.Size).Take(info.Size).ToList(),
            };
        }
        public bool Clear(ApiControllerParamsInfo param)
        {
            loggers.Clear();
            return true;
        }


        public LoggerSetParamInfo GetConfig(ApiControllerParamsInfo param)
        {
            return new LoggerSetParamInfo
            {
                LoggerType = config.Data.Common.LoggerType,
                Size = config.Data.Common.LoggerSize
            };
        }

        public bool SetConfig(ApiControllerParamsInfo param)
        {
            LoggerSetParamInfo info = param.Content.DeJson<LoggerSetParamInfo>();
            config.Data.Common.LoggerSize = info.Size;
            config.Data.Common.LoggerType = info.LoggerType;
            config.Save();
            return true;
        }

    }

    public sealed class LoggerPageInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Type { get; set; }
        public int Count { get; set; }
        public List<LoggerModel> List { get; set; }
    }

    public sealed class LoggerPageParamInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Type { get; set; }
    }

    public sealed class LoggerSetParamInfo
    {
        public LoggerTypes LoggerType { get; set; }
        public int Size { get; set; }
    }
}
