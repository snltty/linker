using linker.libs;
using linker.libs.web;
using System.Reflection;

namespace linker.messenger.api
{
    public interface IWebServer : libs.web.IWebServer
    {
        /// <summary>
        /// 加载插件
        /// </summary>
        void AddPlugins(List<IApiController> list);
    }
    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public sealed class WebServer : libs.web.WebServer, IWebServer
    {
        private readonly IAccessStore accessStore;
        public WebServer(IWebServerFileReader webServerFileReader, IAccessStore accessStore) : base(webServerFileReader)
        {
            this.accessStore = accessStore;
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        public void AddPlugins(List<IApiController> list)
        {
            Type voidType = typeof(void);

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add api {string.Join(",", list.Select(c => c.GetType().Name))}");

            foreach (IApiController obj in list)
            {
                Type type = obj.GetType();
                string path = type.Name.Replace("ApiController", "").Replace("ApiController", "");
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (plugins.ContainsKey(key) == false)
                    {
                        bool istask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null;
                        bool isTaskResult = method.ReturnType.GetProperty("Result") != null;

                        AccessAttribute accessAttr = method.GetCustomAttribute<AccessAttribute>();
                        int access = (int)(accessAttr?.Value ?? 0);

                        plugins.TryAdd(key, new PluginPathCacheInfo
                        {
                            IsVoid = method.ReturnType == voidType,
                            Method = method,
                            Target = obj,
                            IsTask = istask,
                            IsTaskResult = isTaskResult,
                            Access = access,
                            HasAccess = HasAccess,
                        });
                    }
                }
            }
        }
        private bool HasAccess(int access)
        {
            return accessStore.HasAccess(AccessValue.Api) && accessStore.HasAccess((AccessValue)access);
        }
    }

}
