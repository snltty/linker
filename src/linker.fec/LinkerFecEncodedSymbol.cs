using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace linker.fec;

/// <summary>
/// A single transportable FEC symbol.
/// </summary>
public sealed class LinkerFecEncodedSymbol
{
    public const int HeaderSize = 13;

    internal const uint MaxBlockLength = 0x00FF_FFFF;
    internal const int RepairLengthSymbolSize = sizeof(ushort);

    private const byte Magic = 0x52; // 'R'
    private const byte Version = 8;
    private const byte VersionShift = 4;
    private const byte FlagsMask = 0x0F;
    private const byte FinalBlockFlag = 1 << 0;
    private const byte RepairFlag = 1 << 1;

    private const int MagicOffset = 0;
    private const int VersionFlagsOffset = 1;
    private const int BlockIdOffset = 2;
    private const int BlockLengthOffset = 6;
    private const int SourceSymbolCountOffset = 9;
    private const int SymbolIdOffset = 10;
    private const int PayloadLengthOffset = 11;

    public LinkerFecEncodedSymbol(
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlyMemory<byte> payload)
        : this(
            blockId,
            blockLength,
            symbolSize,
            sourceSymbolCount,
            repairSymbolCount,
            symbolId,
            isFinalBlock,
            payload,
            checked((ushort)payload.Length))
    {
    }

    private LinkerFecEncodedSymbol(
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlyMemory<byte> payload,
        ushort lengthSymbol)
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
        IsRepair = symbolId >= sourceSymbolCount;
        Payload = payload;
        LengthSymbol = lengthSymbol;
    }

    private LinkerFecEncodedSymbol(
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        bool isRepair,
        ReadOnlyMemory<byte> payload,
        ushort lengthSymbol)
    {
        BlockId = blockId;
        BlockLength = blockLength;
        SymbolSize = symbolSize;
        SourceSymbolCount = sourceSymbolCount;
        RepairSymbolCount = repairSymbolCount;
        SymbolId = symbolId;
        IsFinalBlock = isFinalBlock;
        IsRepair = isRepair;
        Payload = payload;
        LengthSymbol = lengthSymbol;
    }

    public ulong BlockId { get; }

    public int BlockLength { get; }

    public int SymbolSize { get; }

    public int SourceSymbolCount { get; }

    public int RepairSymbolCount { get; }

    public int SymbolId { get; }

    public bool IsFinalBlock { get; }

    public bool IsRepair { get; }

    public ReadOnlyMemory<byte> Payload { get; }

    internal ushort LengthSymbol { get; }

    /// <summary>
    /// Serializes the symbol into a compact binary frame.
    /// </summary>
    public byte[] ToArray()
    {
        var frame = new byte[HeaderSize + (IsRepair ? RepairLengthSymbolSize : 0) + Payload.Length];
        if (SourceSymbolCount == 0 && !IsRepair)
        {
            WriteStreamingSourceFrame(frame, BlockId, SymbolId, Payload.Span);
            return frame;
        }

        WriteFrame(
            frame,
            BlockId,
            BlockLength,
            SymbolSize,
            SourceSymbolCount,
            RepairSymbolCount,
            SymbolId,
            IsFinalBlock,
            Payload.Span,
            LengthSymbol);
        return frame;
    }

    internal static void WriteFrame(
        Span<byte> frame,
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlySpan<byte> payload)
    {
        WriteFrame(
            frame,
            blockId,
            blockLength,
            symbolSize,
            sourceSymbolCount,
            repairSymbolCount,
            symbolId,
            isFinalBlock,
            payload,
            checked((ushort)payload.Length));
    }

    internal static void WriteFrame(
        Span<byte> frame,
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlySpan<byte> payload,
        ushort lengthSymbol)
    {
        var repairMetadataLength = symbolId >= sourceSymbolCount ? RepairLengthSymbolSize : 0;
        if (frame.Length < HeaderSize + repairMetadataLength + payload.Length)
        {
            throw new ArgumentException("Destination frame buffer is too small.", nameof(frame));
        }

        if (repairMetadataLength != 0)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(frame.Slice(HeaderSize, RepairLengthSymbolSize), lengthSymbol);
        }

        payload.CopyTo(frame.Slice(HeaderSize + repairMetadataLength, payload.Length));
        WriteHeader(
            frame,
            blockId,
            blockLength,
            symbolSize,
            sourceSymbolCount,
            repairSymbolCount,
            symbolId,
            isFinalBlock,
            payload.Length);
    }

    internal static void WriteHeader(
        Span<byte> frame,
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        int payloadLength)
    {
        if (frame.Length < HeaderSize + payloadLength)
        {
            throw new ArgumentException("Destination frame buffer is too small.", nameof(frame));
        }

        Validate(blockLength, symbolSize, sourceSymbolCount, repairSymbolCount, symbolId, payloadLength);
        WriteHeaderUnchecked(
            frame,
            blockId,
            checked((uint)blockLength),
            checked((uint)sourceSymbolCount),
            checked((uint)repairSymbolCount),
            checked((uint)symbolId),
            isFinalBlock,
            checked((uint)payloadLength));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteHeaderUnchecked(
        Span<byte> frame,
        ulong blockId,
        uint blockLength,
        uint sourceSymbolCount,
        uint repairSymbolCount,
        uint symbolId,
        bool isFinalBlock,
        uint payloadLength)
    {
        ValidateHeaderValues(blockLength, sourceSymbolCount, repairSymbolCount, symbolId);
        if (payloadLength > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLength), payloadLength, "Payload length exceeds the compact frame limit.");
        }

        var isRepair = symbolId >= sourceSymbolCount;
        ref var header = ref MemoryMarshal.GetReference(frame);
        Unsafe.Add(ref header, MagicOffset) = Magic;
        Unsafe.Add(ref header, VersionFlagsOffset) = (byte)((Version << VersionShift) |
            (isFinalBlock ? FinalBlockFlag : 0) |
            (isRepair ? RepairFlag : 0));
        WriteUInt32LittleEndian(ref header, BlockIdOffset, unchecked((uint)blockId));
        WriteUInt24LittleEndian(ref header, BlockLengthOffset, blockLength);
        Unsafe.Add(ref header, SourceSymbolCountOffset) = (byte)sourceSymbolCount;
        Unsafe.Add(ref header, SymbolIdOffset) = (byte)symbolId;
        WriteUInt16LittleEndian(ref header, PayloadLengthOffset, checked((ushort)payloadLength));
    }

    internal static void WriteStreamingSourceFrame(
        Span<byte> frame,
        ulong blockId,
        int symbolId,
        ReadOnlySpan<byte> payload)
    {
        if (symbolId is < 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(symbolId), symbolId, "Symbol id must fit in one byte.");
        }

        if (payload.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), payload.Length, "Payload length exceeds the compact frame limit.");
        }

        if (frame.Length < HeaderSize + payload.Length)
        {
            throw new ArgumentException("Destination frame buffer is too small.", nameof(frame));
        }

        ref var header = ref MemoryMarshal.GetReference(frame);
        Unsafe.Add(ref header, MagicOffset) = Magic;
        Unsafe.Add(ref header, VersionFlagsOffset) = (byte)(Version << VersionShift);
        WriteUInt32LittleEndian(ref header, BlockIdOffset, unchecked((uint)blockId));
        WriteUInt24LittleEndian(ref header, BlockLengthOffset, 0);
        Unsafe.Add(ref header, SourceSymbolCountOffset) = 0;
        Unsafe.Add(ref header, SymbolIdOffset) = checked((byte)symbolId);
        WriteUInt16LittleEndian(ref header, PayloadLengthOffset, checked((ushort)payload.Length));
        payload.CopyTo(frame.Slice(HeaderSize, payload.Length));
    }

    public static LinkerFecEncodedSymbol Parse(ReadOnlySpan<byte> frame)
    {
        return Parse(frame, new LinkerFecOptions());
    }

    public static LinkerFecEncodedSymbol Parse(ReadOnlySpan<byte> frame, LinkerFecOptions options)
    {
        if (!TryParse(frame, options, out var symbol, out var error))
        {
            throw new FormatException(error);
        }

        return symbol;
    }

    public static LinkerFecEncodedSymbol Parse(in ReadOnlySequence<byte> frame)
    {
        return Parse(frame, new LinkerFecOptions());
    }

    public static LinkerFecEncodedSymbol Parse(in ReadOnlySequence<byte> frame, LinkerFecOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!TryGetFrameLength(frame, out var frameLength, out var error))
        {
            throw new FormatException(error.Length == 0 ? "Incomplete FEC frame." : error);
        }

        if (frame.Length != frameLength)
        {
            throw new FormatException("Sequence length does not match a single FEC frame.");
        }

        Span<byte> header = stackalloc byte[HeaderSize];
        frame.Slice(0, HeaderSize).CopyTo(header);
        var payloadLength = ReadPayloadLength(header);
        var repairMetadataLength = GetRepairMetadataLength(header);
        ushort lengthSymbol;
        if (repairMetadataLength == 0)
        {
            lengthSymbol = checked((ushort)payloadLength);
        }
        else
        {
            Span<byte> lengthSymbolBytes = stackalloc byte[RepairLengthSymbolSize];
            frame.Slice(HeaderSize, RepairLengthSymbolSize).CopyTo(lengthSymbolBytes);
            lengthSymbol = ReadUInt16LittleEndian(lengthSymbolBytes, 0);
        }

        var payload = frame.Slice(HeaderSize + repairMetadataLength, payloadLength).ToArray();
        return CreateParsedSymbol(header, options, payload, lengthSymbol);
    }

    public static bool TryGetFrameLength(in ReadOnlySequence<byte> buffer, out int frameLength, out string error)
    {
        frameLength = 0;
        error = string.Empty;
        if (buffer.Length < HeaderSize)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[HeaderSize];
        buffer.Slice(0, HeaderSize).CopyTo(header);
        if (!TryValidateHeaderPrefix(header, out error))
        {
            return false;
        }

        var payloadLength = ReadPayloadLength(header);
        frameLength = checked(HeaderSize + GetRepairMetadataLength(header) + payloadLength);
        if (buffer.Length < frameLength)
        {
            error = "Incomplete FEC frame.";
            return false;
        }

        return true;
    }

    public static bool TryGetFrameLength(ReadOnlySpan<byte> frame, out int frameLength, out string error)
    {
        frameLength = 0;
        error = string.Empty;
        if (frame.Length < HeaderSize)
        {
            return false;
        }

        if (!TryValidateHeaderPrefix(frame, out error))
        {
            return false;
        }

        var payloadLength = ReadPayloadLength(frame);
        frameLength = checked(HeaderSize + GetRepairMetadataLength(frame) + payloadLength);
        if (frame.Length < frameLength)
        {
            error = "Incomplete FEC frame.";
            return false;
        }

        return true;
    }

    public static bool TryGetFrameLength(ReadOnlySpan<byte> frame, out int frameLength)
    {
        return TryGetFrameLength(frame, out frameLength, out _);
    }

    internal static byte ReadFlags(ReadOnlySpan<byte> buffer)
    {
        return (byte)(buffer[VersionFlagsOffset] & FlagsMask);
    }

    internal static ulong ReadBlockId(ReadOnlySpan<byte> buffer)
    {
        return ReadUInt32LittleEndian(buffer, BlockIdOffset);
    }

    internal static uint ReadBlockSequence(ReadOnlySpan<byte> buffer)
    {
        return ReadUInt32LittleEndian(buffer, BlockIdOffset);
    }

    internal static int ReadBlockLength(ReadOnlySpan<byte> buffer)
    {
        return checked((int)ReadUInt24LittleEndian(buffer, BlockLengthOffset));
    }

    internal static int ReadSymbolId(ReadOnlySpan<byte> buffer)
    {
        return buffer[SymbolIdOffset];
    }

    internal static int ReadSourceSymbolCount(ReadOnlySpan<byte> buffer)
    {
        return buffer[SourceSymbolCountOffset];
    }

    internal static int ReadPayloadLength(ReadOnlySpan<byte> frame)
    {
        return frame.Length >= HeaderSize
            ? ReadUInt16LittleEndian(frame, PayloadLengthOffset)
            : 0;
    }

    internal static ushort ReadLengthSymbol(ReadOnlySpan<byte> frame)
    {
        return ReadLengthSymbol(frame, frame);
    }

    internal static bool IsRepairFrame(ReadOnlySpan<byte> frame)
    {
        return (ReadFlags(frame) & RepairFlag) != 0;
    }

    internal static int GetPayloadOffset(ReadOnlySpan<byte> frame)
    {
        return HeaderSize + GetRepairMetadataLength(frame);
    }

    private static ushort ReadLengthSymbol(ReadOnlySpan<byte> header, ReadOnlySpan<byte> frame)
    {
        if (GetRepairMetadataLength(header) == 0)
        {
            return checked((ushort)ReadPayloadLength(header));
        }

        return ReadUInt16LittleEndian(frame, HeaderSize);
    }

    private static int GetRepairMetadataLength(ReadOnlySpan<byte> header)
    {
        return IsRepairFrame(header)
            ? RepairLengthSymbolSize
            : 0;
    }

    internal static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        var value = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), offset));
        return BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    internal static uint ReadUInt24LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        ref var reference = ref MemoryMarshal.GetReference(buffer);
        return Unsafe.Add(ref reference, offset) |
            ((uint)Unsafe.Add(ref reference, offset + 1) << 8) |
            ((uint)Unsafe.Add(ref reference, offset + 2) << 16);
    }

    internal static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        var value = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), offset));
        return BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    internal static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> buffer, int offset)
    {
        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), offset));
        return BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt24LittleEndian(ref byte reference, int offset, uint value)
    {
        Unsafe.Add(ref reference, offset) = (byte)value;
        Unsafe.Add(ref reference, offset + 1) = (byte)(value >> 8);
        Unsafe.Add(ref reference, offset + 2) = (byte)(value >> 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt16LittleEndian(ref byte reference, int offset, ushort value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, offset), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteUInt32LittleEndian(ref byte reference, int offset, uint value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, offset), value);
    }

    public static bool TryParse(ReadOnlySpan<byte> frame, out LinkerFecEncodedSymbol symbol)
    {
        return TryParse(frame, new LinkerFecOptions(), out symbol!, out _);
    }

    public static bool TryParse(ReadOnlySpan<byte> frame, out LinkerFecEncodedSymbol symbol, out string error)
    {
        return TryParse(frame, new LinkerFecOptions(), out symbol!, out error);
    }

    public static bool TryParse(
        ReadOnlySpan<byte> frame,
        LinkerFecOptions options,
        out LinkerFecEncodedSymbol symbol,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        symbol = null!;
        error = string.Empty;

        if (!TryGetFrameLength(frame, out var frameLength, out error))
        {
            if (error.Length == 0)
            {
                error = "Frame is shorter than the FEC header.";
            }

            return false;
        }

        if (frame.Length != frameLength)
        {
            error = "Frame length does not match a single FEC frame.";
            return false;
        }

        try
        {
            var header = frame[..HeaderSize];
            var payloadLength = ReadPayloadLength(header);
            var lengthSymbol = ReadLengthSymbol(header, frame);
            symbol = CreateParsedSymbol(
                header,
                options,
                frame.Slice(GetPayloadOffset(header), payloadLength).ToArray(),
                lengthSymbol);
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException or FormatException)
        {
            error = ex.Message;
            symbol = null!;
            return false;
        }

        return true;
    }

    private static LinkerFecEncodedSymbol CreateParsedSymbol(
        ReadOnlySpan<byte> header,
        LinkerFecOptions options,
        ReadOnlyMemory<byte> payload,
        ushort lengthSymbol)
    {
        var blockLength = ReadBlockLength(header);
        var symbolSize = options.SymbolSize;
        var sourceSymbolCount = ReadSourceSymbolCount(header);
        var isRepair = (ReadFlags(header) & RepairFlag) != 0;
        if (sourceSymbolCount == 0)
        {
            if (isRepair)
            {
                throw new FormatException("Streaming source frame cannot be marked as repair.");
            }

            if ((ReadFlags(header) & FinalBlockFlag) != 0)
            {
                throw new FormatException("Streaming source frame cannot mark a final block.");
            }

            var streamingSymbolId = ReadSymbolId(header);
            if (streamingSymbolId >= options.SourceSymbolsPerBlock)
            {
                throw new FormatException("Streaming source symbol id exceeds configured source symbol limit.");
            }

            if (payload.Length > options.SymbolSize)
            {
                throw new FormatException("Streaming source payload exceeds the configured symbol size.");
            }

            return new LinkerFecEncodedSymbol(
                ReadBlockId(header),
                blockLength,
                symbolSize,
                sourceSymbolCount,
                repairSymbolCount: 0,
                streamingSymbolId,
                isFinalBlock: false,
                isRepair: false,
                payload,
                lengthSymbol);
        }

        if (sourceSymbolCount > options.SourceSymbolsPerBlock)
        {
            throw new FormatException($"Source symbol count {sourceSymbolCount} exceeds configured limit.");
        }

        var repairSymbolCount = options.GetRepairSymbolsForSourceCount(sourceSymbolCount);
        var symbolId = ReadSymbolId(header);
        var symbol = new LinkerFecEncodedSymbol(
            ReadBlockId(header),
            blockLength,
            symbolSize,
            sourceSymbolCount,
            repairSymbolCount,
            symbolId,
            (ReadFlags(header) & FinalBlockFlag) != 0,
            payload,
            lengthSymbol);

        ValidateRepairFlag(header, symbol);
        return symbol;
    }

    private static void ValidateRepairFlag(ReadOnlySpan<byte> header, LinkerFecEncodedSymbol symbol)
    {
        if (((ReadFlags(header) & RepairFlag) != 0) != symbol.IsRepair)
        {
            throw new FormatException("Repair flag does not match the symbol id.");
        }
    }

    private static void Validate(int blockLength, int symbolSize, int sourceSymbolCount, int repairSymbolCount, int symbolId, int payloadLength)
    {
        if (blockLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), blockLength, "Block length cannot be negative.");
        }

        if (blockLength > MaxBlockLength)
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), blockLength, "Block length exceeds the compact frame limit.");
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

        if (blockLength > (long)sourceSymbolCount * (symbolSize + sizeof(int)))
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), blockLength, "Decoded record list length exceeds source symbol capacity.");
        }

        if (payloadLength > symbolSize)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLength), payloadLength, "Payload cannot exceed symbol size.");
        }

        if (payloadLength > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadLength), payloadLength, "Payload length exceeds the compact frame limit.");
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

    private static void ValidateHeaderValues(
        uint blockLength,
        uint sourceSymbolCount,
        uint repairSymbolCount,
        uint symbolId)
    {
        if (blockLength > MaxBlockLength)
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), blockLength, "Block length exceeds the compact frame limit.");
        }

        if (sourceSymbolCount is 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSymbolCount), sourceSymbolCount, "Invalid source symbol count.");
        }

        if (repairSymbolCount is 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(repairSymbolCount), repairSymbolCount, "Invalid repair symbol count.");
        }

        if (sourceSymbolCount + repairSymbolCount > LinkerFecOptions.MaxSymbolsPerBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(repairSymbolCount), "The compact frame format supports at most 256 total source and repair symbols per block.");
        }

        if (symbolId >= sourceSymbolCount + repairSymbolCount || symbolId > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(symbolId), symbolId, "Symbol id is outside the block.");
        }
    }

    private static bool TryValidateHeaderPrefix(ReadOnlySpan<byte> header, out string error)
    {
        error = string.Empty;
        if (header[MagicOffset] != Magic)
        {
            error = "Invalid FEC magic.";
            return false;
        }

        var version = header[VersionFlagsOffset] >> VersionShift;
        if (version != Version)
        {
            error = $"Unsupported FEC frame version {version}.";
            return false;
        }

        return true;
    }
}
