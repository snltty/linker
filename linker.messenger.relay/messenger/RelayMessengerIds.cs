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

        Max = 2199
    }
}
