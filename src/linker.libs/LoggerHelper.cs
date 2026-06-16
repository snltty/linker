using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace linker.libs
{
    public sealed class LoggerHelper
    {
        private static readonly Lazy<LoggerHelper> lazy = new Lazy<LoggerHelper>(() => new LoggerHelper());
        public static LoggerHelper Instance => lazy.Value;

        private readonly Channel<LoggerInfo> queue = Channel.CreateUnbounded<LoggerInfo>();
        public Action<LoggerInfo> OnLogger { get; set; } = (param) => { };

        public int PaddingWidth { get; set; } = 50;
#if DEBUG
        public LoggerTypes LoggerLevel { get; set; } = LoggerTypes.DEBUG;
#else
        public LoggerTypes LoggerLevel { get; set; } = LoggerTypes.WARNING;
#endif

        private LoggerHelper()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    LoggerInfo model = await queue.Reader.ReadAsync().ConfigureAwait(false);
                    if (model != null)
                    {
                        OnLogger?.Invoke(model);
                    }
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
            Enqueue(new LoggerInfo { Type = LoggerTypes.DEBUG, Content = content });
        }
        public void Info(string content, params object[] args)
        {
            lock (this)
            {
                if (args != null && args.Length > 0)
                {
                    content = string.Format(content, args);
                }
                Enqueue(new LoggerInfo { Type = LoggerTypes.INFO, Content = content });
            }
        }
        public void Warning(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerInfo { Type = LoggerTypes.WARNING, Content = content });
        }
        public void Warning(Exception ex)
        {
            Enqueue(new LoggerInfo { Type = LoggerTypes.WARNING, Content = ex + "" });
        }
        public void Error(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerInfo { Type = LoggerTypes.ERROR, Content = content });
        }
        public void Error(Exception ex)
        {
            Enqueue(new LoggerInfo { Type = LoggerTypes.ERROR, Content = ex + "" });
        }

        public void FATAL(string content, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                content = string.Format(content, args);
            }
            Enqueue(new LoggerInfo { Type = LoggerTypes.FATAL, Content = content });
        }
        public void FATAL(Exception ex)
        {
            Enqueue(new LoggerInfo { Type = LoggerTypes.FATAL, Content = ex + "" });
        }

        public void Enqueue(LoggerInfo model)
        {
            if (string.IsNullOrWhiteSpace(model.Content)) return;
            queue.Writer.TryWrite(model);
        }
    }

    public sealed class LoggerInfo
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