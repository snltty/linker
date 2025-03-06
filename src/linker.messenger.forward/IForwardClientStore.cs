namespace linker.messenger.forward
{
    public interface IForwardClientStore
    {
        /// <summary>
        /// 获取数量
        /// </summary>
        /// <returns></returns>
        public int Count();

        /// <summary>
        /// 获取端口转发列表
        /// </summary>
        /// <returns></returns>
        public List<ForwardInfo> Get();
        /// <summary>
        /// 根据id获取
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ForwardInfo Get(long id);
        /// <summary>
        /// 根据分组获取
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public List<ForwardInfo> Get(string groupId);
        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Add(ForwardInfo info);
        /// <summary>
        /// 更新转发
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Update(ForwardInfo info);
        /// <summary>
        /// 删除转发
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Remove(long id);

        /// <summary>
        /// 提交更新
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
