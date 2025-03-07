namespace linker.messenger.relay.messenger
{
    public enum RelayMessengerIds : ushort
    {
        Min = 2100,

        Relay = 2101,
        RelayForward = 2102,

        RelayAsk = 2103,
        RelayConfirm = 2104,

        RelayTest = 2105,

        NodeDelay = 2106,
        NodeDelayForward = 2107,

        AddCdkey = 2108,
        PageCdkey = 2109,
        DelCdkey = 2110,
        AccessCdkey = 2111,


        NodeGetCache = 2112,
        NodeReport = 2113,
        TrafficReport = 2114,

        TestCdkey = 2115,
        ImportCdkey = 2116,

        Max = 2199
    }
}
