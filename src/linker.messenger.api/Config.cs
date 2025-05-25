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
    public enum AccessValue : ulong
    {
        None = 0,

        [AccessDisplay("简单管理")]
        NetManager = 1,
        [AccessDisplay("专业管理")]
        FullManager = 1 << 1,

        [AccessDisplay("服务器配置")]
        Config = 1 << 2,

        [AccessDisplay("日志列表")]
        LoggerShow = 1 << 3,
        [AccessDisplay("日志配置")]
        LoggerLevel = 1 << 4,

        [AccessDisplay("修改本机设备名")]
        RenameSelf = 1 << 5,
        [AccessDisplay("修改所有设备名")]
        RenameOther = 1 << 6,
        [AccessDisplay("显示公网信息")]
        ExternalShow = 1 << 7,

        [AccessDisplay("升级本机")]
        UpdateSelf = 1 << 8,
        [AccessDisplay("升级所有设备")]
        UpdateOther = 1 << 9,
        [AccessDisplay("升级服务器")]
        UpdateServer = 1 << 10,

        [AccessDisplay("开关本机网卡")]
        TuntapStatusSelf = 1 << 11,
        [AccessDisplay("开关所有网卡")]
        TuntapStatusOther = 1 << 12,
        [AccessDisplay("修改本机网卡")]
        TuntapChangeSelf = 1 << 13,
        [AccessDisplay("修改所有网卡")]
        TuntapChangeOther = 1 << 14,

        [AccessDisplay("显示本机端口转发")]
        ForwardShowSelf = 1 << 15,
        [AccessDisplay("显示所有设备端口转发")]
        ForwardShowOther = 1 << 16,
        [AccessDisplay("配置本机端口转发")]
        ForwardSelf = 1 << 17,
        [AccessDisplay("配置所有设备端口转发")]
        ForwardOther = 1 << 18,

        [AccessDisplay("重启其它设备")]
        Reboot = 1 << 19,
        [AccessDisplay("删除其它设备")]
        Remove = 1 << 20,

        [AccessDisplay("修改本机网关")]
        TunnelChangeSelf = 1 << 21,
        [AccessDisplay("修改所有设备网关")]
        TunnelChangeOther = 1 << 22,
        [AccessDisplay("删除隧道连接")]
        TunnelRemove = 1 << 23,

        [AccessDisplay("开启管理API")]
        Api = 1 << 24,
        [AccessDisplay("开启管理网页")]
        Web = 1 << 25,

        [AccessDisplay("导出配置")]
        Export = 1 << 26,

        [AccessDisplay("修改权限")]
        Access = 1 << 27,

        [AccessDisplay("修改打洞协议")]
        Transport = 1 << 28,

        [AccessDisplay("修改验证参数")]
        Action = 1 << 29,

        [AccessDisplay("查看内网穿透流量")]
        SForwardFlow = 1 << 30,

        [AccessDisplay("查看中继流量")]
        RelayFlow = (ulong)1 << 31,

        [AccessDisplay("查看信标流量")]
        SigninFlow = (ulong)1 << 32,

        [AccessDisplay("查看流量")]
        Flow = (ulong)1 << 33,

        [AccessDisplay("同步配置")]
        Sync = (ulong)1 << 34,

        [AccessDisplay("配置组网网络")]
        Lease = (ulong)1 << 35,

        [AccessDisplay("开关本机socks5")]
        Socks5StatusSelf = (ulong)1 << 36,
        [AccessDisplay("开关所有socks5")]
        Socks5StatusOther = (ulong)1 << 37,
        [AccessDisplay("修改本机socks5")]
        Socks5ChangeSelf = (ulong)1 << 38,
        [AccessDisplay("修改所有socks5")]
        Socks5ChangeOther = (ulong)1 << 39,

        [AccessDisplay("配置分组")]
        Group = (ulong)1 << 40,

        [AccessDisplay("重置本机接口密码")]
        SetApiPassword = (ulong)1 << 41,
        [AccessDisplay("重置所有接口密码")]
        SetApiPasswordOther = (ulong)1 << 42,

        [AccessDisplay("管理中继CDKEY")]
        RelayCdkey = (ulong)1 << 43,

        [AccessDisplay("管理本机防火墙")]
        FirewallSelf = (ulong)1 << 44,
        [AccessDisplay("管理所有设备防火墙")]
        FirewallOther = (ulong)1 << 45,

        [AccessDisplay("唤醒本机")]
        WakeupSelf = (ulong)1 << 46,
        [AccessDisplay("唤醒所有设备")]
        WakeupOther = (ulong)1 << 47,

        Full = ulong.MaxValue >> 64 - 52,
    }

    public sealed class AccessTextInfo
    {
        public ulong Value { get; set; }
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
