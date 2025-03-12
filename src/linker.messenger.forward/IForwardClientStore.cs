using System.Net;

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
        public IEnumerable<ForwardInfo> Get();
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
        public IEnumerable<ForwardInfo> Get(string groupId);
        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Add(ForwardInfo info);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="started"></param>
        /// <param name="proxy"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Update(long id,bool started,bool proxy,string msg);
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
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Update(long id, string msg);
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Update(long id, int port);
        public bool Update(string machineId, IPEndPoint target, string targetMsg);
        /// <summary>
        /// 删除转发
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Remove(long id);
    }
}
