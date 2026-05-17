namespace linker.fec;

/// <summary>
/// Identifies how a decoded application packet was produced.
/// </summary>
public enum LinkerFecDecodedPacketKind
{
    /// <summary>
    /// The packet was emitted directly from a received source frame.
    /// </summary>
    Source = 0,

    /// <summary>
    /// The packet was reconstructed from FEC repair data.
    /// </summary>
    Recovered = 1
}
