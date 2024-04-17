using cmonitor.plugins.signin.messenger;
using cmonitor.server;
using common.libs.extends;
using System.Collections.Concurrent;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxyCaching
    {
        private readonly ConcurrentDictionary<string, string> viewerCache = new ConcurrentDictionary<string, string>();


        private readonly SignCaching signCaching;
        public ViewerProxyCaching(SignCaching signCaching)
        {
            this.signCaching = signCaching;
        }

        public string Set(string serverMachineName)
        {
            while (true)
            {
                string id = GenerateUniqueId();
                if (viewerCache.TryAdd(id, serverMachineName))
                {
                    return id;
                }
            }
        }
        public bool Remove(string id)
        {
            return viewerCache.TryRemove(id, out _);
        }

        public bool Get(string id, out IConnection connection)
        {
            connection = null;
            if (viewerCache.TryGetValue(id, out string serverMachineName))
            {
                if (signCaching.Get(serverMachineName, out SignCacheInfo info))
                {
                    connection = info.Connection;
                    return connection != null && connection.Connected;
                }
            }
            return false;
        }


        private string GenerateUniqueId()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            return Convert.ToBase64String(bytes).SubStr(0, 9);
        }
    }
}
