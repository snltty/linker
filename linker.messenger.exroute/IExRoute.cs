using System.Net;

namespace linker.messenger.exroute
{
    public interface IExRoute
    {
        /// <summary>
        /// 获取排除的路由
        /// </summary>
        /// <returns></returns>
        public List<IPAddress> Get();
    }
}
