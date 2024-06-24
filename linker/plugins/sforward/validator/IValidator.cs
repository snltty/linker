using linker.plugins.sforward.config;
using linker.server;

namespace linker.plugins.sforward.validator
{
    public interface IValidator
    {
        public bool Valid(IConnection connection, SForwardAddInfo sForwardAddInfo,out string error);
    }
}
