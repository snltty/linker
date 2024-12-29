using linker.libs;
using linker.messenger.access;
using linker.messenger.api;
using System.Reflection;

namespace linker.config
{
    public partial class ConfigClientInfo
    {
        /// <summary>
        /// 客户端管理接口配置
        /// </summary>
        public ApiClientInfo CApi { get; set; } = new ApiClientInfo();

        private Dictionary<string, AccessTextInfo> accesss;
        public Dictionary<string, AccessTextInfo> Accesss
        {
            get
            {
                if (accesss == null)
                {
                    accesss = new Dictionary<string, AccessTextInfo>();
                    Type enumType = typeof(AccessValue);
                    // 获取所有字段值
                    foreach (var value in Enum.GetValues(enumType))
                    {
                        // 获取字段信息
                        FieldInfo fieldInfo = enumType.GetField(value.ToString());
                        var attribute = fieldInfo.GetCustomAttribute<AccessDisplayAttribute>(false);
                        if (attribute != null)
                        {
                            accesss.TryAdd(fieldInfo.Name, new AccessTextInfo { Text = attribute.Value, Value = (ulong)value });
                        }
                    }
                }

                return accesss;
            }
        }
        /// <summary>
        /// 管理权限
        /// </summary>
        public AccessValue Access { get; set; } = AccessValue.Full;
    }
}
