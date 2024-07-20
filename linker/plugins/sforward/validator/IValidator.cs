using linker.plugins.messenger;
using linker.plugins.sforward.config;

namespace linker.plugins.sforward.validator
{
    public interface IValidator
    {
        public bool Valid(IConnection connection, SForwardAddInfo sForwardAddInfo,out string error);
    }
}
