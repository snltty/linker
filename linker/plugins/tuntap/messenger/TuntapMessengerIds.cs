namespace linker.plugins.tuntap.messenger
{
    public enum TuntapMessengerIds : ushort
    {
        Run = 2200,
        RunForward = 2201,

        Stop = 2202,
        StopForward = 2203,

        Update = 2204,
        UpdateForward = 2205,

        Config = 2206,
        ConfigForward = 2207,


        LeaseAdd = 2208,
        LeaseGet = 2209,
        Lease = 2210,
        LeaseChange = 2211,
        LeaseChangeForward = 2212,

        None = 2299
    }
}
