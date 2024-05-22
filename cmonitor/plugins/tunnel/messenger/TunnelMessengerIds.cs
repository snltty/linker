namespace cmonitor.plugins.tunnel.messenger
{
    public enum TunnelMessengerIds : ushort
    {
        _ = 2000,

        Info = 2001,
        InfoForward = 2002,

        Begin = 2003,
        BeginForward = 2004,

        Fail = 2005,
        FailForward = 2006,

        Success = 2007,
        SuccessForward = 2008,

        Config = 2009,
        ConfigForward = 2010,

        Update = 2011,
        UpdateForward = 2012,

        None = 2099
    }
}
