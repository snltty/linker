namespace linker.messenger.sforward.client
{
    public interface ISForwardClientStore
    {
        /// <summary>
        /// 穿透密钥
        /// </summary>
        public string SecretKey { get;  }

        /// <summary>
        /// 设置穿透密钥
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool SetSecretKey(string key);

        /// <summary>
        /// 穿透数量
        /// </summary>
        /// <returns></returns>
        public int Count();

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <returns></returns>
        public List<SForwardInfo> Get();
        /// <summary>
        /// 获取穿透
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SForwardInfo Get(uint id);
        /// <summary>
        /// 获取穿透
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public SForwardInfo Get(string domain);
        /// <summary>
        /// 获取穿透
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public SForwardInfo Get(int port);
        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Add(SForwardInfo info);
        /// <summary>
        /// 更新穿透
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Update(SForwardInfo info);
        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Remove(uint id);
        /// <summary>
        /// 提交保存
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
