namespace linker.stun;

public static class StunConstants
{
    public const ushort BindingRequest = 0x0001;
    public const ushort BindingSuccessResponse = 0x0101;
    public const ushort BindingErrorResponse = 0x0111;

    public const uint MagicCookie = 0x2112A442;
    public const int HeaderSize = 20;
    public const int TransactionIdLength = 12;

    public const ushort AttributeMappedAddress = 0x0001;
    public const ushort AttributeChangeRequest = 0x0003;
    public const ushort AttributeErrorCode = 0x0009;
    public const ushort AttributeUnknownAttributes = 0x000A;
    public const ushort AttributeXorMappedAddress = 0x0020;
    public const ushort AttributeResponsePort = 0x0027;
    public const ushort AttributeSoftware = 0x8022;
    public const ushort AttributeAlternateServer = 0x8023;
    public const ushort AttributeFingerprint = 0x8028;
    public const ushort AttributeResponseOrigin = 0x802B;
    public const ushort AttributeOtherAddress = 0x802C;
}
