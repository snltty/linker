namespace linker.messenger.tuntap.client
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
