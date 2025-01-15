using linker.messenger.exroute;
using System.Net;

namespace linker.messenger.signin
{
    public sealed class SignInExRoute : IExRoute
    {
        private readonly SignInClientState signInClientState;
        public SignInExRoute(SignInClientState signInClientState)
        {
            this.signInClientState = signInClientState;
        }
        public List<IPAddress> Get()
        {
            return new List<IPAddress> { signInClientState.Connection?.Address.Address ?? IPAddress.Any, signInClientState.WanAddress.Address };
        }
    }
}
