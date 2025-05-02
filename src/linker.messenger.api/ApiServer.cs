using linker.libs;
using linker.libs.api;
using System.Reflection;

namespace linker.messenger.api
{
    /// <summary>
    /// 前段接口服务
    /// </summary>
    public sealed partial class ApiServer : libs.api.ApiServer, IApiServer
    {
        private readonly IAccessStore accessStore;
        public ApiServer(IAccessStore accessStore)
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
                        ulong access = 0;
                        if (accessAttr != null)
                        {
                            access = (ulong)accessAttr.Value;
                        }
                        
                        plugins.TryAdd(key, new PluginPathCacheInfo
                        {
                            IsVoid = method.ReturnType == voidType,
                            Method = method,
                            Target = obj,
                            IsTask = istask,
                            IsTaskResult = isTaskResult,
                            Access = 0,
                            HasAccess = HasAccess,
                        });
                    }
                }
            }
        }
        private bool HasAccess(ulong access)
        {
            return accessStore.HasAccess((AccessValue)access);
        }
    }
}
