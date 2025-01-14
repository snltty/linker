using linker.libs;

namespace linker.messenger.logger
{
    public interface ILoggerStore
    {
        /// <summary>
        /// 日志等级
        /// </summary>
        public LoggerTypes LoggerType { get; }
        /// <summary>
        /// 日志最大存储量
        /// </summary>
        public int LoggerSize { get; }

        /// <summary>
        /// 设置等级
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool SetLevel(LoggerTypes level);
        /// <summary>
        /// 设置存储量
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool SetSize(int size);
        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
