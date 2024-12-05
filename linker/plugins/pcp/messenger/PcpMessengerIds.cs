namespace linker.plugins.pcp.messenger
{
    public enum PcpMessengerIds : ushort
    {
        _ = 3100,

        Begin = 3101,
        BeginForward = 3102,

        Fail = 3103,
        FailForward = 3104,

        Success = 3105,
        SuccessForward = 3106,

        None = 3199
    }
}
