using System;

namespace linker.kcp;

internal sealed class KcpSegment
{
    public uint Conv;
    public byte Command;
    public byte Fragment;
    public ushort Window;
    public uint Timestamp;
    public uint Sn;
    public uint Una;
    public uint Rto;
    public uint Transmit;
    public uint ResendTimestamp;
    public uint FastAck;
    public bool FastResendPending;
    public bool Acked;
    public byte[]? Buffer;
    public int Length;

    public void Reset()
    {
        Conv = 0;
        Command = 0;
        Fragment = 0;
        Window = 0;
        Timestamp = 0;
        Sn = 0;
        Una = 0;
        Rto = 0;
        Transmit = 0;
        ResendTimestamp = 0;
        FastAck = 0;
        FastResendPending = false;
        Acked = false;
        Buffer = null;
        Length = 0;
    }

    public void EnsureCapacity(int length)
    {
        if (Buffer is null)
        {
            throw new InvalidOperationException("Segment buffer has already been recycled.");
        }

        if (Buffer.Length >= length)
        {
            return;
        }

        var newBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
        Buffer.AsSpan(0, Length).CopyTo(newBuffer);
        System.Buffers.ArrayPool<byte>.Shared.Return(Buffer);
        Buffer = newBuffer;
    }
}
