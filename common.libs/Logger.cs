using System;
using System.Collections.Concurrent;
using System.Threading;

namespace common.libs
{
    public sealed class Logger
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => lazy.Value;

        private readonly ConcurrentQueue<LoggerModel> queue = new ConcurrentQueue<LoggerModel>();
        public Action<LoggerModel> OnLogger { get; set; } = (param) => { };

        public int PaddingWidth { get; set; } = 50;
#if DEBUG
        public LoggerTypes LoggerLevel { get; set; } = LoggerTypes.DEBUG;
#else
        public LoggerTypes LoggerLevel { get; set; } = LoggerTypes.WARNING;
#endif

        private Logger()
        {
            new Thread(() =>
            {
                while (true)
                {
                    while (queue.Count > 0)
                    {
                        if (queue.TryDequeue(out LoggerModel model))
                        {
                            OnLogger?.Invoke(model);
                        }
                    }
                    Thread.Sleep(15);
                }
            })
            { IsBackground = true }.Start();
        }


        public int lockNum = 0;
        public void Lock()
        {
            Interlocked.Increment(ref lockNum);
        }
        public void UnLock()
        {
            Interlocked.Decrement(ref lockNum);
        }

        public void Debug(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.DEBUG, Content = content });
        }
        public void Info(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.INFO, Content = content });
        }
        public void Warning(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = content });
        }
        public void Error(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = content });
        }
        public void Error(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.ERROR, Content = ex + "" });
        }

        public void FATAL(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerModel { Type = LoggerTypes.FATAL, Content = content });
        }
        public void FATAL(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.FATAL, Content = ex + "" });
        }

        public void Enqueue(LoggerModel model)
        {
            queue.Enqueue(model);
        }
    }

    public sealed class LoggerModel
    {
        public LoggerTypes Type { get; set; } = LoggerTypes.INFO;
        public DateTime Time { get; set; } = DateTime.Now;
        public string Content { get; set; } = string.Empty;
    }

    public enum LoggerTypes : byte
    {
        DEBUG = 0,
        INFO = 1,
        WARNING = 2,
        ERROR = 3,
        FATAL = 4,
    }
}