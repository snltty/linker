using System;
using System.Buffers;

namespace linker.fec.Internal;

internal struct ReceivedSymbol : IDisposable
{
    private byte[]? _ownedBuffer;
    private ReadOnlyMemory<byte> _payload;

    private ReceivedSymbol(
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlyMemory<byte> payload,
        ushort lengthSymbol,
        byte[]? ownedBuffer)
    {
        Validate(blockLength, symbolSize, sourceSymbolCount, repairSymbolCount, symbolId, payload.Length);
        if (symbolId >= sourceSymbolCount &&
            !IsValidRepairPayloadLength(blockLength, symbolSize, sourceSymbolCount, payload.Length))
        {
            throw new ArgumentException("Repair payload length is invalid for the source block.", nameof(payload));
        }

        BlockId = blockId;
        BlockLength = blockLength;
        SymbolSize = symbolSize;
        SourceSymbolCount = sourceSymbolCount;
        RepairSymbolCount = repairSymbolCount;
        SymbolId = symbolId;
        IsFinalBlock = isFinalBlock;
        LengthSymbol = lengthSymbol;
        _payload = payload;
        _ownedBuffer = ownedBuffer;
    }

    public ulong BlockId { get; }

    public int BlockLength { get; }

    public int SymbolSize { get; }

    public int SourceSymbolCount { get; }

    public int RepairSymbolCount { get; }

    public int SymbolId { get; }

    public bool IsFinalBlock { get; }

    public bool IsRepair => SymbolId >= SourceSymbolCount;

    public bool IsAssigned => SymbolSize != 0;

    public ushort LengthSymbol { get; }

    public ReadOnlySpan<byte> PayloadSpan => _payload.Span;

    public static ReceivedSymbol ParsePooled(in ReadOnlySequence<byte> frame)
    {
        return ParsePooled(frame, new LinkerFecOptions());
    }

    public static ReceivedSymbol ParsePooled(in ReadOnlySequence<byte> frame, LinkerFecOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!LinkerFecEncodedSymbol.TryGetFrameLength(frame, out var frameLength, out var error))
        {
            throw new FormatException(error.Length == 0 ? "Incomplete FEC frame." : error);
        }

        if (frame.Length != frameLength)
        {
            throw new FormatException("Sequence length does not match a single FEC frame.");
        }

        Span<byte> header = stackalloc byte[LinkerFecEncodedSymbol.HeaderSize];
        frame.Slice(0, LinkerFecEncodedSymbol.HeaderSize).CopyTo(header);
        var payloadOffset = LinkerFecEncodedSymbol.GetPayloadOffset(header);
        var payloadLength = checked((int)frame.Length) - payloadOffset;
        ushort lengthSymbol;
        if (payloadOffset == LinkerFecEncodedSymbol.HeaderSize)
        {
            lengthSymbol = checked((ushort)payloadLength);
        }
        else
        {
            Span<byte> lengthSymbolBytes = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
            frame.Slice(LinkerFecEncodedSymbol.HeaderSize, LinkerFecEncodedSymbol.RepairLengthSymbolSize).CopyTo(lengthSymbolBytes);
            lengthSymbol = LinkerFecEncodedSymbol.ReadUInt16LittleEndian(lengthSymbolBytes, 0);
        }

        var rented = payloadLength == 0 ? Array.Empty<byte>() : ArrayPool<byte>.Shared.Rent(payloadLength);
        var shouldReturnRented = payloadLength != 0;

        try
        {
            var payload = rented.AsSpan(0, payloadLength);
            frame.Slice(payloadOffset, payloadLength).CopyTo(payload);
            var symbol = CreateSymbol(
                header,
                options,
                rented.AsMemory(0, payloadLength),
                lengthSymbol,
                payloadLength == 0 ? null : rented);
            shouldReturnRented = false;
            return symbol;
        }
        catch
        {
            if (shouldReturnRented)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            throw;
        }
    }

    public static ReceivedSymbol ParsePooled(ReadOnlySpan<byte> frame)
    {
        return ParsePooled(frame, new LinkerFecOptions());
    }

    public static ReceivedSymbol ParsePooled(ReadOnlySpan<byte> frame, LinkerFecOptions options)
    {
        if (!LinkerFecEncodedSymbol.TryGetFrameLength(frame, out _, out var error))
        {
            throw new FormatException(error.Length == 0 ? "Incomplete FEC frame." : error);
        }

        return ParsePooled(frame, LinkerFecEncodedSymbol.ReadBlockId(frame), options);
    }

    public static ReceivedSymbol ParsePooled(ReadOnlySpan<byte> frame, ulong blockId)
    {
        return ParsePooled(frame, blockId, new LinkerFecOptions());
    }

    public static ReceivedSymbol ParsePooled(ReadOnlySpan<byte> frame, ulong blockId, LinkerFecOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!LinkerFecEncodedSymbol.TryGetFrameLength(frame, out var frameLength, out var error))
        {
            throw new FormatException(error.Length == 0 ? "Incomplete FEC frame." : error);
        }

        if (frame.Length != frameLength)
        {
            throw new FormatException("Frame length does not match a single FEC frame.");
        }

        var header = frame[..LinkerFecEncodedSymbol.HeaderSize];
        var payloadLength = LinkerFecEncodedSymbol.ReadPayloadLength(frame);
        var payload = frame.Slice(LinkerFecEncodedSymbol.GetPayloadOffset(frame), payloadLength);
        var rented = payloadLength == 0 ? Array.Empty<byte>() : ArrayPool<byte>.Shared.Rent(payloadLength);
        var shouldReturnRented = payloadLength != 0;
        try
        {
            if (payloadLength != 0)
            {
                payload.CopyTo(rented);
            }

            var symbol = CreateSymbol(
                blockId,
                header,
                options,
                rented.AsMemory(0, payloadLength),
                LinkerFecEncodedSymbol.ReadLengthSymbol(frame),
                payloadLength == 0 ? null : rented);
            shouldReturnRented = false;
            return symbol;
        }
        catch
        {
            if (shouldReturnRented)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            throw;
        }
    }

    public static ReceivedSymbol CreatePooledSource(
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlySpan<byte> payload)
    {
        var rented = payload.Length == 0 ? Array.Empty<byte>() : ArrayPool<byte>.Shared.Rent(payload.Length);
        var shouldReturnRented = payload.Length != 0;
        try
        {
            if (payload.Length != 0)
            {
                payload.CopyTo(rented);
            }

            var symbol = new ReceivedSymbol(
                blockId,
                blockLength,
                symbolSize,
                sourceSymbolCount,
                repairSymbolCount,
                symbolId,
                isFinalBlock,
                rented.AsMemory(0, payload.Length),
                checked((ushort)payload.Length),
                payload.Length == 0 ? null : rented);
            shouldReturnRented = false;
            return symbol;
        }
        finally
        {
            if (shouldReturnRented)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    public void Dispose()
    {
        var buffer = _ownedBuffer;
        _ownedBuffer = null;
        _payload = default;
        if (buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static ReceivedSymbol CreateSymbol(
        ReadOnlySpan<byte> header,
        LinkerFecOptions options,
        ReadOnlyMemory<byte> payload,
        ushort lengthSymbol,
        byte[]? ownedBuffer)
    {
        return CreateSymbol(LinkerFecEncodedSymbol.ReadBlockId(header), header, options, payload, lengthSymbol, ownedBuffer);
    }

    private static ReceivedSymbol CreateSymbol(
        ulong blockId,
        ReadOnlySpan<byte> header,
        LinkerFecOptions options,
        ReadOnlyMemory<byte> payload,
        ushort lengthSymbol,
        byte[]? ownedBuffer)
    {
        var sourceSymbolCount = LinkerFecEncodedSymbol.ReadSourceSymbolCount(header);
        if (sourceSymbolCount > options.SourceSymbolsPerBlock)
        {
            throw new FormatException($"Source symbol count {sourceSymbolCount} exceeds configured limit.");
        }

        var blockLength = LinkerFecEncodedSymbol.GetDecodedRecordCapacity(sourceSymbolCount, options.SymbolSize);
        var symbol = new ReceivedSymbol(
            blockId,
            blockLength,
            options.SymbolSize,
            sourceSymbolCount,
            options.GetRepairSymbolsForSourceCount(sourceSymbolCount),
            LinkerFecEncodedSymbol.ReadSymbolId(header),
            (LinkerFecEncodedSymbol.ReadFlags(header) & 1) != 0,
            payload,
            lengthSymbol,
            ownedBuffer);

        if (((LinkerFecEncodedSymbol.ReadFlags(header) & 2) != 0) != symbol.IsRepair)
        {
            symbol.Dispose();
            throw new FormatException("Repair flag does not match the symbol id.");
        }

        return symbol;
    }

    private static void Validate(int blockLength, int symbolSize, int sourceSymbolCount, int repairSymbolCount, int symbolId, int payloadLength)
    {
        if (blockLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), blockLength, "Block length cannot be negative.");
        }

        if (symbolSize is < LinkerFecOptions.MinSymbolSize or > LinkerFecOptions.MaxSymbolSize)
        {
            throw new ArgumentOutOfRangeException(nameof(symbolSize), symbolSize, "Invalid symbol size.");
        }

        if (sourceSymbolCount is < LinkerFecOptions.MinSourceSymbolsPerBlock or > LinkerFecOptions.MaxSourceSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSymbolCount), sourceSymbolCount, "Invalid source symbol count.");
        }

        if (repairSymbolCount is < LinkerFecOptions.MinRepairSymbolsPerBlock or > LinkerFecOptions.MaxRepairSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(repairSymbolCount), repairSymbolCount, "Invalid repair symbol count.");
        }

        if (sourceSymbolCount + repairSymbolCount > LinkerFecOptions.MaxSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(repairSymbolCount), "The compact frame format supports at most 256 total source and repair symbols per block.");
        }

        if (symbolId < 0 || symbolId >= sourceSymbolCount + repairSymbolCount)
        {
            throw new ArgumentOutOfRangeException(nameof(symbolId), symbolId, "Symbol id is outside the block.");
        }

        if (blockLength > (long)sourceSymbolCount * (symbolSize + LinkerFecOptions.RecordLengthPrefixSize))
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), blockLength, "Decoded record list length exceeds source symbol capacity.");
        }

        if (payloadLength > symbolSize)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLength), payloadLength, "Payload cannot exceed symbol size.");
        }
    }

    private static bool IsValidRepairPayloadLength(
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int payloadLength)
    {
        return payloadLength <= symbolSize;
    }
}
