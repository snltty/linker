namespace linker.messenger.tuntap.messenger
{
    public enum TuntapMessengerIds : ushort
    {
        Run = 2200,
        RunForward = 2201,

        Stop = 2202,
        StopForward = 2203,

        Update = 2204,
        UpdateForward = 2205,


        LeaseAddNetwork = 2208,
        LeaseGetNetwork = 2209,
        LeaseIP = 2210,
        LeaseChange = 2211,
        LeaseChangeForward = 2212,
        LeaseExp = 2213,

        SubscribeForwardTest = 2214,
        SubscribeForwardTestForward = 2215,


        Routes = 2216,
        RoutesForward = 2217,

        ID = 2218,
        IDForward = 2219,

        SetID = 2220,
        SetIDForward = 2221,

        None = 2299
    }
}
