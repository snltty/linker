using cmonitor.plugins.sforward.config;
using cmonitor.server;

namespace cmonitor.plugins.sforward.validator
{
    public interface IValidator
    {
        public bool Valid(IConnection connection, SForwardAddInfo sForwardAddInfo,out string error);
    }
}
