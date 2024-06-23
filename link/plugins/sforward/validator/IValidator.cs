using link.plugins.sforward.config;
using link.server;

namespace link.plugins.sforward.validator
{
    public interface IValidator
    {
        public bool Valid(IConnection connection, SForwardAddInfo sForwardAddInfo,out string error);
    }
}
