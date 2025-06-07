namespace linker.messenger.wakeup
{
    public enum WakeupMessengerIds : ushort
    {
        Start = 3400,

        Get = 3401,
        GetForward = 3402,

        Add = 3403,
        AddForward = 3404,
        Remove = 3405,
        RemoveForward = 3406,
        Send = 3407,
        SendForward = 3408,

        Coms = 3409,
        ComsForward = 3410,

        Hids = 3411,
        HidsForward = 3412,

        End = 3499
    }
}
