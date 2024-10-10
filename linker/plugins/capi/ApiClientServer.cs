using linker.config;
using linker.libs;
using linker.libs.api;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace linker.plugins.capi
{
    /// <summary>
    /// 前段接口服务
    /// </summary>
    public sealed partial class ApiClientServer : ApiServer, IApiClientServer
    {
        private readonly ServiceProvider serviceProvider;
        private readonly FileConfig config;

        public ApiClientServer(ServiceProvider serviceProvider, FileConfig config)
        {
            this.serviceProvider = serviceProvider;
            this.config = config;

            LoadPlugins();
        }

        /// <summary>
        /// 加载插件
        /// </summary>
        private void LoadPlugins()
        {
            Type voidType = typeof(void);

            IEnumerable<Type> types = GetSourceGeneratorTypes();

            foreach (Type item in types)
            {
                object obj = serviceProvider.GetService(item);
                if (obj == null)
                {
                    continue;
                }
                LoggerHelper.Instance.Info($"load client api:{item.Name}");

                string path = item.Name.Replace("ApiController", "").Replace("ApiController", "");
                foreach (MethodInfo method in item.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
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
            return config.Data.Client.HasAccess((ClientApiAccess)access);
        }
    }
}
