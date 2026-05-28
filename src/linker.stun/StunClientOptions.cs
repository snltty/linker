using System;
using System.Net;

namespace linker.stun;

public sealed class StunClientOptions
{
    public StunAddressFamilyMode AddressFamilyMode { get; init; } = StunAddressFamilyMode.Ipv4Preferred;

    public IPEndPoint? LocalEndPoint { get; init; }

    public TimeSpan InitialRto { get; init; } = TimeSpan.FromMilliseconds(500);

    public int MaxAttempts { get; init; } = 7;

    public int ReceiveBufferSize { get; init; } = 4096;

    public string? Software { get; init; } = "linker.stun";

    internal void Validate()
    {
        if (InitialRto <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialRto), "Initial RTO must be positive.");
        }

        if (MaxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxAttempts), "MaxAttempts must be positive.");
        }

        if (ReceiveBufferSize < StunConstants.HeaderSize)
        {
            throw new ArgumentOutOfRangeException(nameof(ReceiveBufferSize), "ReceiveBufferSize is too small.");
        }
    }
}
