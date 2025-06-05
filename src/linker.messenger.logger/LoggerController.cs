using linker.libs.extends;
using linker.libs;
using linker.messenger.api;
using linker.libs.web;

namespace linker.messenger.logger
{
    public sealed class LoggerApiController : IApiController
    {
        private readonly List<LoggerModel> loggers = new List<LoggerModel>();
        private readonly ILoggerStore loggerStore;
        public LoggerApiController(ILoggerStore loggerStore)
        {
            this.loggerStore = loggerStore;
            LoggerHelper.Instance.OnLogger += (LoggerModel logger) =>
            {
                loggers.Add(logger);
                if (loggers.Count > loggerStore.LoggerSize)
                {
                    loggers.RemoveAt(0);
                }
            };
        }

        [Access(AccessValue.LoggerShow)]
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
                LoggerType = loggerStore.LoggerType,
                Size = loggerStore.LoggerSize
            };
        }

        [Access(AccessValue.LoggerLevel)]
        public bool SetConfig(ApiControllerParamsInfo param)
        {
            LoggerSetParamInfo info = param.Content.DeJson<LoggerSetParamInfo>();
            loggerStore.SetLevel(info.LoggerType);
            loggerStore.SetSize(info.Size);
            loggerStore.Confirm();
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
