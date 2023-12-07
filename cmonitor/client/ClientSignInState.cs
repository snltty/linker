using cmonitor.service;

namespace cmonitor.client
{
    public sealed class ClientSignInState
    {
        public IConnection Connection { get; set; }
        public bool Connected => Connection != null && Connection.Connected;


    }
}
