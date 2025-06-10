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


        NodeGetCache = 2112,
        NodeReport = 2113,
        TrafficReport = 2114,

        UpdateNode = 2117,
        UpdateNodeForward = 2118,

        RelayTest170 = 2119,
        RelayAsk170 = 2120,
        RelayForward170 = 2121,

        SendLastBytes = 2122,

        CheckKey = 2123,

        Max = 2199
    }
}
