
namespace linker.messenger.tuntap
{
    public interface ITuntapClientStore
    {
        public TuntapConfigInfo Info{ get; }

        public void Confirm();
    }
}
