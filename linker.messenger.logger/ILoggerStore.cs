using linker.libs;

namespace linker.messenger.logger
{
    public interface ILoggerStore
    {
        public LoggerTypes LoggerType { get; }
        public int LoggerSize { get; }

        public bool SetLevel(LoggerTypes level);
        public bool SetSize(int size);

        public bool Confirm();
    }
}
