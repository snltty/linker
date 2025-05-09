namespace linker.messenger.firewall
{
    public enum FirewallMessengerIds : ushort
    {
        Min = 3300,

        Get = 3301,
        GetForward = 3302,

        Add = 3303,
        AddForward = 3304,
        Remove = 3305,
        RemoveForward = 3306,

        State = 3307,
        StateForward = 3308,

        Max = 3399
    }
}
