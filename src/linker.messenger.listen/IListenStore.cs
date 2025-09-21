namespace linker.messenger.listen
{
    public interface IListenStore
    {
        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; }
        public int ApiPort { get; }
        /// <summary>
        /// 设置端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool SetPort(int port);
        public bool SetApiPort(int port);
        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
