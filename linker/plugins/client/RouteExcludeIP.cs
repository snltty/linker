using linker.plugins.route;
using System.Net;

namespace linker.plugins.client
{
    public sealed class RouteExcludeIPSignin : IRouteExcludeIP
    {
        private readonly ClientSignInState clientSignInState;
        public RouteExcludeIPSignin(ClientSignInState clientSignInState)
        {
            this.clientSignInState = clientSignInState;
        }
        public List<IPAddress> Get()
        {
            return new List<IPAddress> { clientSignInState.Connection?.Address.Address ?? IPAddress.Any };
        }
    }
}
