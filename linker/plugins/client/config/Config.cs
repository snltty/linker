using linker.config;
using linker.libs;
using linker.libs.extends;
using LiteDB;
using MemoryPack;
using System.Net;
using System.Reflection;
using System.Text;

namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public ClientRunningInfo Client { get; set; } = new ClientRunningInfo();
    }

    public sealed class ClientRunningInfo
    {
        public ObjectId Id { get; set; }
        public ClientServerInfo[] Servers { get; set; } = [];
    }
}

namespace linker.config
{
    public sealed partial class ConfigInfo
    {
        public ConfigClientInfo Client { get; set; } = new ConfigClientInfo();
    }

    public sealed partial class ConfigClientInfo : IConfig
    {
        private ICrypto crypto;
        public ConfigClientInfo()
        {
            crypto = CryptoFactory.CreateSymmetric(Helper.GlobalString);

            accesss?.Clear();
        }


        private Dictionary<string, ClientApiAccessInfo> accesss;
        public Dictionary<string, ClientApiAccessInfo> Accesss
        {
            get
            {
                if (accesss == null)
                {
                    accesss = new Dictionary<string, ClientApiAccessInfo>();
                    Type enumType = typeof(ClientApiAccess);
                    // 获取所有字段值
                    foreach (var value in Enum.GetValues(enumType))
                    {
                        // 获取字段信息
                        FieldInfo fieldInfo = enumType.GetField(value.ToString());
                        var attribute = fieldInfo.GetCustomAttribute<ClientAccessDisplayAttribute>(false);
                        if (attribute != null)
                        {
                            accesss.TryAdd(fieldInfo.Name, new ClientApiAccessInfo { Text = attribute.Value, Value = (ulong)value });
                        }
                    }
                }

                return accesss;
            }
        }
        /// <summary>
        /// 管理权限
        /// </summary>
        public ClientApiAccess Access { get; set; } = ClientApiAccess.Full;
        /// <summary>
        /// 是否拥有某项权限
        /// </summary>
        /// <param name="clientManagerAccess"></param>
        /// <returns></returns>
        public bool HasAccess(ClientApiAccess clientManagerAccess)
        {
            return (Access & clientManagerAccess) == clientManagerAccess;
        }
        public bool OnlyNode { get; set; }


        private ClientServerInfo[] servers = new ClientServerInfo[] {
#if DEBUG
            new ClientServerInfo{ Name="Linker", Host=new IPEndPoint(IPAddress.Loopback, 1802).ToString() }
#else
            new ClientServerInfo{ Name="Linker", Host="linker.snltty.com:1802" }
#endif
        };
        public ClientServerInfo[] Servers
        {
            get => servers; set
            {
                servers = value;
            }
        }
        public ClientServerInfo ServerInfo => servers[0];


        private string id = string.Empty;
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
            }
        }

        private string name = Dns.GetHostName().SubStr(0, 12);
        public string Name
        {
            get => name; set
            {
                name = value.SubStr(0, 12);
            }
        }

#if DEBUG
        private string groupid = "snltty";
#else
        private string groupid = string.Empty;
#endif
        public string GroupId
        {
            get => groupid; set
            {
                groupid = value.SubStr(0, 36);
            }
        }

        public string Certificate { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";

        public string Serialize(object obj)
        {
#if DEBUG
            return obj.ToJsonFormat();
#else
            return Convert.ToBase64String(crypto.Encode(Encoding.UTF8.GetBytes(obj.ToJson())));
#endif
        }

        public object Deserialize(string text)
        {
            if (text.Contains("ApiPassword"))
            {
                return text.DeJson<ConfigClientInfo>();
            }
            return Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(text)).ToArray()).DeJson<ConfigClientInfo>();
        }
    }

    [MemoryPackable]
    public sealed partial class ClientServerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;

        public string ToStr()
        {
            return $"{Host}-{SecretKey}";
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ClientApiAccessAttribute : Attribute
    {
        public ClientApiAccess Value { get; set; }

        public ClientApiAccessAttribute(ClientApiAccess value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class ClientAccessDisplayAttribute : Attribute
    {
        public string Value { get; set; }

        public ClientAccessDisplayAttribute(string value)
        {
            Value = value;
        }
    }

    [Flags]
    public enum ClientApiAccess : ulong
    {
        None = 0,

        [ClientAccessDisplayAttribute("简单管理")]
        NetManager = 1,
        [ClientAccessDisplayAttribute("专业管理")]
        FullManager = 1 << 1,

        [ClientAccessDisplayAttribute("服务器配置")]
        Config = 1 << 2,

        [ClientAccessDisplayAttribute("日志列表")]
        LoggerShow = 1 << 3,
        [ClientAccessDisplayAttribute("日志配置")]
        LoggerLevel = 1 << 4,

        [ClientAccessDisplayAttribute("修改本机设备名")]
        RenameSelf = 1 << 5,
        [ClientAccessDisplayAttribute("修改所有设备名")]
        RenameOther = 1 << 6,
        [ClientAccessDisplayAttribute("显示公网信息")]
        ExternalShow = 1 << 7,

        [ClientAccessDisplayAttribute("升级本机")]
        UpdateSelf = 1 << 8,
        [ClientAccessDisplayAttribute("升级所有设备")]
        UpdateOther = 1 << 9,
        [ClientAccessDisplayAttribute("升级服务器")]
        UpdateServer = 1 << 10,

        [ClientAccessDisplayAttribute("开关本机网卡")]
        TuntapStatusSelf = 1 << 11,
        [ClientAccessDisplayAttribute("开关所有网卡")]
        TuntapStatusOther = 1 << 12,
        [ClientAccessDisplayAttribute("修改本机网卡")]
        TuntapChangeSelf = 1 << 13,
        [ClientAccessDisplayAttribute("修改所有网卡")]
        TuntapChangeOther = 1 << 14,

        [ClientAccessDisplayAttribute("显示本机端口转发")]
        ForwardShowSelf = 1 << 15,
        [ClientAccessDisplayAttribute("显示所有设备端口转发")]
        ForwardShowOther = 1 << 16,
        [ClientAccessDisplayAttribute("配置本机端口转发")]
        ForwardSelf = 1 << 17,
        [ClientAccessDisplayAttribute("配置所有设备端口转发")]
        ForwardOther = 1 << 18,

        [ClientAccessDisplayAttribute("重启其它设备")]
        Reboot = 1 << 19,
        [ClientAccessDisplayAttribute("删除其它设备")]
        Remove = 1 << 20,

        [ClientAccessDisplayAttribute("修改本机网关")]
        TunnelChangeSelf = 1 << 21,
        [ClientAccessDisplayAttribute("修改所有设备网关")]
        TunnelChangeOther = 1 << 22,
        [ClientAccessDisplayAttribute("删除隧道连接")]
        TunnelRemove = 1 << 23,

        [ClientAccessDisplayAttribute("开启管理API")]
        Api = 1 << 24,
        [ClientAccessDisplayAttribute("开启管理网页")]
        Web = 1 << 25,

        [ClientAccessDisplayAttribute("导出配置")]
        Export = 1 << 26,

        [ClientAccessDisplayAttribute("修改权限")]
        Access = 1 << 27,

        [ClientAccessDisplayAttribute("修改打洞协议")]
        Transport = 1 << 28,

        [ClientAccessDisplayAttribute("修改验证参数")]
        Action = 1 << 29,

        [ClientAccessDisplayAttribute("查看内网穿透流量")]
        SForwardFlow = 1 << 30,

        [ClientAccessDisplayAttribute("查看中继流量")]
        RelayFlow = ((ulong)1 << 31),

        Full = ulong.MaxValue >> (64 - 52),
    }

    public sealed class ClientApiAccessInfo
    {
        public ulong Value { get; set; }
        public string Text { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ConfigUpdateAccessInfo
    {
        /// <summary>
        /// 设备
        /// </summary>
        public string FromMachineId { get; set; }
        /// <summary>
        /// 设备
        /// </summary>
        public string ToMachineId { get; set; }
        /// <summary>
        /// 权限
        /// </summary>
        public ulong Access { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ConfigAccessInfo
    {
        /// <summary>
        /// 设备
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 权限
        /// </summary>
        public ClientApiAccess Access { get; set; }
    }
}
