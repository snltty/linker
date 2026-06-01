namespace linker.messenger.reverse.client
{
    public interface IReverseClientStore
    {
        /// <summary>
        /// 穿透数量
        /// </summary>
        /// <returns></returns>
        public int Count();

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ReverseInfo> Get();
        /// <summary>
        /// 获取穿透
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ReverseInfo Get(long id);
        /// <summary>
        /// 获取穿透
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public ReverseInfo Get(string domain);
        /// <summary>
        /// 获取穿透
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public ReverseInfo GetPort(int port);
        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Add(ReverseInfo info);
        /// <summary>
        /// 更新穿透
        /// </summary>
        /// <param name="id"></param>
        /// <param name="started"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Update(long id,bool started,string msg);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="started"></param>
        /// <returns></returns>
        public bool Update(long id,bool started);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="localMsg"></param>
        /// <returns></returns>
        public bool Update(long id,string localMsg);

        public bool UpdateNodeId1(long id, string nodeid1);

        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Remove(long id);
    }
}
