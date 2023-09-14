using cmonitor.server.service;

namespace cmonitor.server.client
{
    public sealed class ClientSignInState
    {
        public IConnection Connection { get; set; }
        public bool Connected => Connection != null && Connection.Connected;


    }
}
