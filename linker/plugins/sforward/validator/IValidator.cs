using Linker.Plugins.SForward.Config;
using Linker.Server;

namespace Linker.Plugins.SForward.Validator
{
    public interface IValidator
    {
        public bool Valid(IConnection connection, SForwardAddInfo sForwardAddInfo,out string error);
    }
}
