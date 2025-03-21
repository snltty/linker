
namespace linker.app
{
    public interface ILinkerVpnService
    {
        public bool Running { get; }

        public void StartVpnService();
        public void StopVpnService();
    }
}
