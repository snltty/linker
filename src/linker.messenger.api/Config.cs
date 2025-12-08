using System.Collections;

namespace linker.messenger.api
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AccessAttribute : Attribute
    {
        public AccessValue Value { get; set; }

        public AccessAttribute(AccessValue value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class AccessDisplayAttribute : Attribute
    {
        public string Value { get; set; }

        public AccessDisplayAttribute(string value)
        {
            Value = value;
        }
    }


    [Flags]
    public enum AccessValue : int
    {
        [AccessDisplay("简单管理")]
        NetManager = 0,
        [AccessDisplay("专业管理")]
        FullManager = 1,

        [AccessDisplay("服务器配置")]
        Config = 2,

        [AccessDisplay("日志列表")]
        LoggerShow = 3,
        [AccessDisplay("日志配置")]
        LoggerLevel = 4,

        [AccessDisplay("修改本机设备名")]
        RenameSelf = 5,
        [AccessDisplay("修改所有设备名")]
        RenameOther = 6,
        [AccessDisplay("显示公网信息")]
        ExternalShow = 7,

        [AccessDisplay("升级本机")]
        UpdateSelf = 8,
        [AccessDisplay("升级所有设备")]
        UpdateOther = 9,
        [AccessDisplay("升级服务器")]
        UpdateServer = 10,

        [AccessDisplay("开关本机网卡")]
        TuntapStatusSelf = 11,
        [AccessDisplay("开关所有网卡")]
        TuntapStatusOther = 12,
        [AccessDisplay("修改本机网卡")]
        TuntapChangeSelf = 13,
        [AccessDisplay("修改所有网卡")]
        TuntapChangeOther = 14,

        [AccessDisplay("显示本机端口转发")]
        ForwardShowSelf = 15,
        [AccessDisplay("显示所有设备端口转发")]
        ForwardShowOther = 16,
        [AccessDisplay("配置本机端口转发")]
        ForwardSelf = 17,
        [AccessDisplay("配置所有设备端口转发")]
        ForwardOther = 18,

        [AccessDisplay("重启其它设备")]
        Reboot = 19,
        [AccessDisplay("删除其它设备")]
        Remove = 20,

        [AccessDisplay("修改本机网关")]
        TunnelChangeSelf = 21,
        [AccessDisplay("修改所有设备网关")]
        TunnelChangeOther = 22,
        [AccessDisplay("删除隧道连接")]
        TunnelRemove = 23,

        [AccessDisplay("开启管理API")]
        Api = 24,
        [AccessDisplay("开启管理网页")]
        Web = 25,

        [AccessDisplay("导出配置")]
        Export = 26,

        [AccessDisplay("修改权限")]
        Access = 27,

        [AccessDisplay("修改打洞协议")]
        Transport = 28,

        [AccessDisplay("修改本机验证参数")]
        Action = 29,

        [AccessDisplay("查看内网穿透流量")]
        SForwardFlow = 30,

        [AccessDisplay("查看中继流量")]
        RelayFlow = 31,

        [AccessDisplay("查看信标流量")]
        SigninFlow = 32,

        [AccessDisplay("查看流量")]
        Flow = 33,

        [AccessDisplay("同步配置")]
        Sync = 34,

        [AccessDisplay("配置组网网络")]
        Lease = 35,

        [AccessDisplay("开关本机socks5")]
        Socks5StatusSelf = 36,
        [AccessDisplay("开关所有socks5")]
        Socks5StatusOther = 37,
        [AccessDisplay("修改本机socks5")]
        Socks5ChangeSelf = 38,
        [AccessDisplay("修改所有socks5")]
        Socks5ChangeOther = 39,

        [AccessDisplay("修改分组")]
        Group = 40,

        [AccessDisplay("修改本机接口密码")]
        SetApiPassword = 41,
        [AccessDisplay("修改所有接口密码")]
        SetApiPasswordOther = 42,

        [AccessDisplay("管理CDKEY")]
        Cdkey = 43,

        [AccessDisplay("修改本机防火墙")]
        FirewallSelf = 44,
        [AccessDisplay("修改所有设备防火墙")]
        FirewallOther = 45,

        [AccessDisplay("唤醒本机")]
        WakeupSelf = 46,
        [AccessDisplay("唤醒所有设备")]
        WakeupOther = 47,

        [AccessDisplay("修改所有验证参数")]
        ActionOther = 48,

        [AccessDisplay("白名单")]
        WhiteList = 49,

        [AccessDisplay("查看端口转发流量")]
        ForwardFlow = 50,
        [AccessDisplay("查看Socks5流量")]
        Socks5Flow = 51,
        [AccessDisplay("查看隧道流量")]
        TunnelFlow = 52,

        [AccessDisplay("导入中继节点")]
        ImportRelayNode = 53,
        [AccessDisplay("删除中继节点")]
        RemoveRelayNode = 54,
        [AccessDisplay("修改中继节点")]
        UpdateRelayNode = 55,
        [AccessDisplay("分享中继节点")]
        ShareRelayNode = 56,
        [AccessDisplay("重启中继节点")]
        RebootRelayNode = 57,
        [AccessDisplay("更新中继节点")]
        UpgradeRelayNode = 58,

        [AccessDisplay("查看其它客户端")]
        FullList = 59,


        [AccessDisplay("导入穿透节点")]
        ImportSForwardNode = 60,
        [AccessDisplay("删除穿透节点")]
        RemoveSForwardNode = 61,
        [AccessDisplay("修改穿透节点")]
        UpdateSForwardNode = 62,
        [AccessDisplay("分享穿透节点")]
        ShareSForwardNode = 63,
        [AccessDisplay("重启穿透节点")]
        RebootSForwardNode = 64,
        [AccessDisplay("更新穿透节点")]
        UpgradeSForwardNode = 65,
    }

    public sealed class AccessTextInfo
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }

    public sealed partial class AccessUpdateInfo
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
    public sealed partial class AccessBitsUpdateInfo
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
        public BitArray Access { get; set; }
        /// <summary>
        /// 顶级满权限
        /// </summary>
        public bool FullAccess { get; set; }
    }

    public sealed partial class AccessInfo
    {
        /// <summary>
        /// 设备
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 权限
        /// </summary>
        public AccessValue Access { get; set; }
    }
    public sealed partial class AccessBitsInfo
    {
        /// <summary>
        /// 设备
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 权限
        /// </summary>
        public BitArray Access { get; set; }
    }


    public sealed partial class ApiPasswordUpdateInfo
    {
        /// <summary>
        /// 设备
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
    }
}
