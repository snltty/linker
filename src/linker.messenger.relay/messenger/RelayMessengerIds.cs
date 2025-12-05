namespace linker.messenger.relay.messenger
{
    public enum RelayMessengerIds : ushort
    {
        Min = 2100,

        Ask = 2103,

        Nodes = 2105,

        GetCache = 2112,
        NodeReport = 2128,

        SignIn = 2134,
        Report = 2135,

        Share = 2136,
        ShareForward = 2137,
        Import = 2138,
        Remove = 2139,

        UpdateForward = 2140,
        Update = 2141,

        ExitForward = 2142,
        Exit = 2143,

        UpgradeForward = 2144,
        Upgrade = 2145,

        Max = 2199
    }
}
