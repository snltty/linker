using linker.config;
using linker.libs.api;
using linker.plugins.access;
using System.Reflection;

namespace linker.plugins.capi
{
    /// <summary>
    /// 前段接口服务
    /// </summary>
    public sealed partial class ApiClientServer : ApiServer, IApiClientServer
    {
        private readonly FileConfig config;
        private readonly AccessTransfer accessTransfer;
        public ApiClientServer(FileConfig config, AccessTransfer accessTransfer)
        {
            this.config = config;
            this.accessTransfer = accessTransfer;
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        public void LoadPlugins(List<object> list)
        {
            Type voidType = typeof(void);

            foreach (object obj in list)
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
                        ClientApiAccessAttribute accessAttr = method.GetCustomAttribute<ClientApiAccessAttribute>();
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
                            Access = access,
                            HasAccess = HasAccess,
                        });
                    }
                }
            }
        }
        private bool HasAccess(ulong access)
        {
            return accessTransfer.HasAccess((ClientApiAccess)access);
        }
    }
}
