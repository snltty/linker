namespace linker.forward
{
    public enum ForwardFlags : byte
    {
        Fin = 0b00000001,
        Syn = 0b00000010,
        Rst = 0b00000100,
        Psh = 0b00001000,
        Ack = 0b00010000,
        Urg = 0b00100000,

        SynAck = Syn | Ack,
        PshAck = Psh | Ack,
        RstAck = Rst | Ack,
    }
}
