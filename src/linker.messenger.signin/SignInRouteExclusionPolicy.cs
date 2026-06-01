using linker.messenger.rpolicy;
using System.Net;

namespace linker.messenger.signin
{
    public sealed class SignInRouteExclusionPolicy : IRouteExclusionPolicy
    {
        private readonly SignInClientState signInClientState;
        public SignInRouteExclusionPolicy(SignInClientState signInClientState)
        {
            this.signInClientState = signInClientState;
        }
        public List<IPAddress> Query()
        {
            return new List<IPAddress> { signInClientState.Connection?.Address.Address ?? IPAddress.Any, signInClientState.WanAddress.Address };
        }
    }
}
