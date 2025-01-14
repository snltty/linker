
namespace linker.messenger.tuntap
{
    public interface ITuntapClientStore
    {
        public TuntapConfigInfo Info{ get; }

        /// <summary>
        /// 提交保存
        /// </summary>
        public void Confirm();
    }
}
