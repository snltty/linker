using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace linker.libs
{
    public sealed class LoggerHelper
    {
        private static readonly Lazy<LoggerHelper> lazy = new Lazy<LoggerHelper>(() => new LoggerHelper());
        public static LoggerHelper Instance => lazy.Value;

        private readonly ConcurrentQueue<LoggerModel> queue = new ConcurrentQueue<LoggerModel>();
        public Action<LoggerModel> OnLogger { get; set; } = (param) => { };

        public int PaddingWidth { get; set; } = 50;
#if DEBUG
        public LoggerTypes LoggerLevel { get; set; } = LoggerTypes.DEBUG;
#else
        public LoggerTypes LoggerLevel { get; set; } = LoggerTypes.WARNING;
#endif

        private LoggerHelper()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    while (queue.IsEmpty == false)
                    {
                        if (queue.TryDequeue(out LoggerModel model))
                        {
                            OnLogger?.Invoke(model);
                        }
                    }
                    Thread.Sleep(15);
                }
            }, TaskCreationOptions.LongRunning);
        }


        public int lockNum;
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
        public void Warning(Exception ex)
        {
            Enqueue(new LoggerModel { Type = LoggerTypes.WARNING, Content = ex + "" });
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
            if (string.IsNullOrWhiteSpace(model.Content)) return;
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