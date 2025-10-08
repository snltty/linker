namespace linker.messenger.relay.messenger
{
    public enum RelayMessengerIds : ushort
    {
        Min = 2100,

        Relay = 2101,
        RelayForward = 2102,

        RelayAsk = 2103,
        RelayConfirm = 2104,

        Nodes = 2105,

        NodeDelay = 2106,
        NodeDelayForward = 2107,


        NodeGetCache = 2112,
        NodeReport = 2113,
        TrafficReport = 2114,

        Edit = 2117,
        EditForward = 2118,

        Nodes170 = 2119,
        RelayAsk170 = 2120,
        RelayForward170 = 2121,

        SendLastBytes = 2122,

        NodeGetCache186 = 2124,

        Exit = 2125,
        ExitForward = 2126,

        Nodes188 = 2127,
        NodeReport188 = 2128,

        Update = 2129,
        UpdateForward = 2130,

        Edit188 = 2131,
        EditForward188 = 2132,

        Hosts = 2133,

        Max = 2199
    }
}
