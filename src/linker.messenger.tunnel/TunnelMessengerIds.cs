namespace linker.messenger.tunnel
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

        RouteLevel = 2011,
        RouteLevelForward = 2012,

        Network = 2013,
        NetworkForward = 2014,

        TransportGet = 2015,
        TransportGetForward = 2016,
        TransportSet = 2017,
        TransportSetForward = 2018,

        None = 2099
    }
}
