using linker.libs;
using linker.messenger.logger;

namespace linker.messenger.store.file.logger
{
    public sealed class LoggerStore : ILoggerStore
    {
        public LoggerTypes LoggerType => fileConfig.Data.Common.LoggerType;

        public int LoggerSize => fileConfig.Data.Common.LoggerSize;

        private readonly FileConfig fileConfig;
        public LoggerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        public bool Confirm()
        {
            fileConfig.Data.Update();
            return true;
        }

        public bool SetLevel(LoggerTypes level)
        {
            fileConfig.Data.Common.LoggerType = level;
            return true;
        }

        public bool SetSize(int size)
        {
            fileConfig.Data.Common.LoggerSize = size;
            return true;
        }
    }
}
