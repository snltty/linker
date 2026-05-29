using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using linker.fec.Internal;

namespace linker.fec;

/// <summary>
/// End-to-end FEC codec.
/// </summary>
public sealed class LinkerFecCodec : IDisposable
{
    private const int RecordLengthPrefixSize = LinkerFecOptions.RecordLengthPrefixSize;
    private const int FrameLengthPrefixSize = LinkerFecOptions.FrameLengthPrefixSize;

    private readonly LinkerFecOptions _options;
    private readonly Dictionary<ulong, DecoderBlock> _decoderBlocks = [];
    private byte[][]? _repairCoefficientCache;
    private int _repairCoefficientSourceCount;
    private byte[]? _intermediateSymbolBuffer;
    private int _intermediateSymbolBufferSourceCount;
    private int _intermediateSymbolBufferSymbolSize;
    private byte _singleSourceRepairCoefficient;
    private bool _hasSingleSourceRepairCoefficient;
    private ulong[]? _decodedWindowBlockIds;
    private int _decodedWindowBlockIdCount;
    private ulong _nextEncodeBlockId;
    private ulong _nextDecodeBlockId;
    private ulong _maxDecodeBlockIdSeen;
    private bool _hasDecodeBlockIdSeen;
    private ulong _finalDecodeBlockId;
    private bool _hasFinalDecodeBlockId;
    private bool _rawCompleted;
    private bool _disposed;

    public LinkerFecCodec(LinkerFecOptions? options = null)
    {
        _options = options ?? new LinkerFecOptions();
        _options.Validate();
    }

    public LinkerFecOptions Options => _options;

    /// <summary>
    /// Gets the total number of application packets recovered by FEC repair decoding.
    /// Packets emitted directly from received source frames are not included.
    /// </summary>
    public long FecRecoveredPacketCount { get; private set; }

    /// <summary>
    /// Encodes one 2-byte length-prefixed application record list synchronously and writes all generated frames into
    /// <paramref name="destination"/> as [2-byte frame length][frame] records.
    /// The record length prefix is little-endian and does not include the 2 prefix bytes.
    /// </summary>
    public int EncodePacket(
        byte[] rawPacket,
        byte[] destination,
        out int packetCount,
        bool isFinalPacket = false)
    {
        ArgumentNullException.ThrowIfNull(rawPacket);
        ArgumentNullException.ThrowIfNull(destination);
        return EncodePacket(rawPacket.AsSpan(), destination.AsSpan(), out packetCount, isFinalPacket);
    }

    /// <summary>
    /// Encodes one 2-byte length-prefixed application record list synchronously and writes all generated frames into
    /// <paramref name="destination"/> as [2-byte frame length][frame] records.
    /// The record length prefix is little-endian and does not include the 2 prefix bytes.
    /// </summary>
    public int EncodePacket(
        ReadOnlyMemory<byte> rawPacket,
        Memory<byte> destination,
        out int packetCount,
        bool isFinalPacket = false)
    {
        return EncodePacket(rawPacket.Span, destination.Span, out packetCount, isFinalPacket);
    }

    /// <summary>
    /// Encodes one 2-byte length-prefixed application record list synchronously and writes all generated frames into
    /// <paramref name="destination"/> as [2-byte frame length][frame] records.
    /// The record length prefix is little-endian and does not include the 2 prefix bytes.
    /// </summary>
    public int EncodePacket(
        ReadOnlySpan<byte> rawPacket,
        Span<byte> destination,
        out int packetCount,
        bool isFinalPacket = false)
    {
        ThrowIfDisposed();

        if (_rawCompleted)
        {
            throw new InvalidOperationException("Application record input has already been completed.");
        }

        ValidateApplicationRecords(rawPacket, nameof(rawPacket));

        if (!TryEncodePacketCore(rawPacket, destination, out var bytesWritten, out packetCount, isFinalPacket))
        {
            throw new ArgumentException("Destination buffer is too small for the encoded packet.", nameof(destination));
        }

        return bytesWritten;
    }

    /// <summary>
    /// Tries to encode one 2-byte length-prefixed application record list synchronously into [2-byte frame length][frame]
    /// records. Returns false if <paramref name="destination"/> is too small.
    /// </summary>
    public bool TryEncodePacket(
        byte[] rawPacket,
        byte[] destination,
        out int bytesWritten,
        out int packetCount,
        bool isFinalPacket = false)
    {
        ArgumentNullException.ThrowIfNull(rawPacket);
        ArgumentNullException.ThrowIfNull(destination);
        return TryEncodePacket(rawPacket.AsSpan(), destination.AsSpan(), out bytesWritten, out packetCount, isFinalPacket);
    }

    /// <summary>
    /// Tries to encode one 2-byte length-prefixed application record list synchronously into [2-byte frame length][frame]
    /// records. Returns false if <paramref name="destination"/> is too small.
    /// </summary>
    public bool TryEncodePacket(
        ReadOnlyMemory<byte> rawPacket,
        Memory<byte> destination,
        out int bytesWritten,
        out int packetCount,
        bool isFinalPacket = false)
    {
        return TryEncodePacket(rawPacket.Span, destination.Span, out bytesWritten, out packetCount, isFinalPacket);
    }

    /// <summary>
    /// Tries to encode one 2-byte length-prefixed application record list synchronously into [2-byte frame length][frame]
    /// records. Returns false if <paramref name="destination"/> is too small.
    /// </summary>
    public bool TryEncodePacket(
        ReadOnlySpan<byte> rawPacket,
        Span<byte> destination,
        out int bytesWritten,
        out int packetCount,
        bool isFinalPacket = false)
    {
        ThrowIfDisposed();

        if (_rawCompleted)
        {
            throw new InvalidOperationException("Application record input has already been completed.");
        }

        ValidateApplicationRecords(rawPacket, nameof(rawPacket));
        return TryEncodePacketCore(rawPacket, destination, out bytesWritten, out packetCount, isFinalPacket);
    }

    private bool TryEncodePacketCore(
        ReadOnlySpan<byte> rawPacket,
        Span<byte> destination,
        out int bytesWritten,
        out int packetCount,
        bool isFinalPacket)
    {
        bytesWritten = 0;
        packetCount = 0;

        var symbolSize = _options.SymbolSize;
        Span<int> sourceOffsets = stackalloc int[_options.SourceSymbolsPerBlock];
        Span<int> sourceLengths = stackalloc int[_options.SourceSymbolsPerBlock];
        var sourceCount = BuildSourceSegments(rawPacket, symbolSize, sourceOffsets, sourceLengths);
        sourceOffsets = sourceOffsets[..sourceCount];
        sourceLengths = sourceLengths[..sourceCount];
        var repairSymbolCount = _options.GetRepairSymbolsForSourceCount(sourceCount);

        if (sourceCount == 1 && repairSymbolCount == 1)
        {
            var singleSourceRequiredSize = GetSingleSourcePacketizedOutputSize(sourceLengths[0]);
            if (destination.Length < singleSourceRequiredSize)
            {
                return false;
            }

            EncodeSingleSourcePacketCore(rawPacket, destination, out bytesWritten, out packetCount, isFinalPacket);
            return true;
        }

        var requiredSize = GetPacketizedOutputSize(sourceLengths, repairSymbolCount);
        if (destination.Length < requiredSize)
        {
            return false;
        }

        EncodePacketCore(
            rawPacket,
            destination,
            sourceOffsets,
            sourceLengths,
            repairSymbolCount,
            requiredSize,
            out bytesWritten,
            out packetCount,
            isFinalPacket);
        return true;
    }

    public int DecodeFrame(ReadOnlyMemory<byte> encodedFrame, Memory<byte> destination)
    {
        return DecodeFrame(encodedFrame.Span, destination.Span);
    }

    public int DecodeFrame(ReadOnlySpan<byte> encodedFrame, Span<byte> destination)
    {
        return TryDecodeFrame(encodedFrame, destination, out var bytesWritten)
            ? bytesWritten
            : 0;
    }

    public bool TryDecodeFrame(
        ReadOnlyMemory<byte> encodedFrame,
        Memory<byte> destination,
        out int bytesWritten)
    {
        return TryDecodeFrame(encodedFrame.Span, destination.Span, out bytesWritten, out _);
    }

    public bool TryDecodeFrame(
        ReadOnlyMemory<byte> encodedFrame,
        Memory<byte> destination,
        out int bytesWritten,
        out int packetCount)
    {
        return TryDecodeFrame(encodedFrame.Span, destination.Span, out bytesWritten, out packetCount);
    }

    /// <summary>
    /// Tries to decode one FEC frame. On success, <paramref name="packetKinds"/> receives one entry per decoded
    /// [2-byte length][payload] record, in the same order as those records are written to <paramref name="destination"/>.
    /// </summary>
    public bool TryDecodeFrame(
        ReadOnlyMemory<byte> encodedFrame,
        Memory<byte> destination,
        Memory<LinkerFecDecodedPacketKind> packetKinds,
        out int bytesWritten,
        out int packetCount)
    {
        return TryDecodeFrame(encodedFrame.Span, destination.Span, packetKinds.Span, out bytesWritten, out packetCount);
    }

    public bool TryDecodeFrame(
        ReadOnlySpan<byte> encodedFrame,
        Span<byte> destination,
        out int bytesWritten)
    {
        return TryDecodeFrame(encodedFrame, destination, out bytesWritten, out _);
    }

    public bool TryDecodeFrame(
        ReadOnlySpan<byte> encodedFrame,
        Span<byte> destination,
        out int bytesWritten,
        out int packetCount)
    {
        return TryDecodeFrameCore(
            encodedFrame,
            destination,
            packetKinds: default,
            writePacketKinds: false,
            out bytesWritten,
            out packetCount);
    }

    /// <summary>
    /// Tries to decode one FEC frame. On success, <paramref name="packetKinds"/> receives one entry per decoded
    /// [2-byte length][payload] record, in the same order as those records are written to <paramref name="destination"/>.
    /// </summary>
    public bool TryDecodeFrame(
        ReadOnlySpan<byte> encodedFrame,
        Span<byte> destination,
        Span<LinkerFecDecodedPacketKind> packetKinds,
        out int bytesWritten,
        out int packetCount)
    {
        return TryDecodeFrameCore(
            encodedFrame,
            destination,
            packetKinds,
            writePacketKinds: true,
            out bytesWritten,
            out packetCount);
    }

    private bool TryDecodeFrameCore(
        ReadOnlySpan<byte> encodedFrame,
        Span<byte> destination,
        Span<LinkerFecDecodedPacketKind> packetKinds,
        bool writePacketKinds,
        out int bytesWritten,
        out int packetCount)
    {
        ThrowIfDisposed();
        bytesWritten = 0;
        packetCount = 0;
        if (!LinkerFecEncodedSymbol.TryGetFrameLength(encodedFrame, out var frameLength, out var error))
        {
            throw new FormatException(error.Length == 0 ? "Incomplete FEC frame." : error);
        }

        if (encodedFrame.Length != frameLength)
        {
            throw new FormatException("Frame length does not match a single FEC frame.");
        }

        var blockId = ExpandDecodeBlockId(LinkerFecEncodedSymbol.ReadBlockSequence(encodedFrame));
        if (_hasFinalDecodeBlockId && blockId > _finalDecodeBlockId)
        {
            throw new InvalidDataException("Received a block after the final FEC block.");
        }

        AdvanceDecodeWindow(blockId);

        if (blockId < _nextDecodeBlockId || HasDecodedWindowBlockId(blockId))
        {
            return false;
        }

        if (TryDecodeSingleSourceFrame(
            encodedFrame,
            blockId,
            destination,
            out bytesWritten,
            out var singleSourceRecoveredPacketCount,
            out var handled))
        {
            packetCount = ValidateDecodedApplicationRecords(destination[..bytesWritten]);
            WriteDecodedPacketKinds(
                packetKinds,
                writePacketKinds,
                packetCount,
                sourcePacketCount: packetCount - singleSourceRecoveredPacketCount,
                recoveredPacketCount: singleSourceRecoveredPacketCount);
            FecRecoveredPacketCount += singleSourceRecoveredPacketCount;
            return true;
        }

        if (handled)
        {
            return false;
        }

        var symbol = ReceivedSymbol.ParsePooled(encodedFrame, blockId, _options);
        if (!TryWriteReceivedSymbol(symbol, out var block))
        {
            return false;
        }

        if (!symbol.IsRepair)
        {
            bytesWritten = WriteSourceSymbolRecord(symbol, destination);
            var fecRecoveredPacketCount = 0;
            if (block!.CanDecode)
            {
                if (block.MissingSourceCount == 0)
                {
                    CompleteDecodedBlock(blockId, block);
                }
                else
                {
                    var missingSourceCount = block.MissingSourceCount;
                    if (block.TryDecodeMissing(destination.Slice(bytesWritten), out var recoveredBytes))
                    {
                        bytesWritten += recoveredBytes;
                        fecRecoveredPacketCount = missingSourceCount;
                        CompleteDecodedBlock(blockId, block);
                    }
                }
            }

            packetCount = ValidateDecodedApplicationRecords(destination[..bytesWritten]);
            WriteDecodedPacketKinds(
                packetKinds,
                writePacketKinds,
                packetCount,
                sourcePacketCount: packetCount - fecRecoveredPacketCount,
                recoveredPacketCount: fecRecoveredPacketCount);
            FecRecoveredPacketCount += fecRecoveredPacketCount;
            return true;
        }

        if (block is not null && block.MissingSourceCount == 0)
        {
            CompleteDecodedBlock(blockId, block);
            return false;
        }

        if (!TryDecodeReceivedBlock(blockId, destination, out bytesWritten, out var recoveredPacketCount))
        {
            return false;
        }

        packetCount = ValidateDecodedApplicationRecords(destination[..bytesWritten]);
        WriteDecodedPacketKinds(
            packetKinds,
            writePacketKinds,
            packetCount,
            sourcePacketCount: 0,
            recoveredPacketCount: recoveredPacketCount);
        FecRecoveredPacketCount += recoveredPacketCount;
        return true;
    }

    private bool TryWriteReceivedSymbol(ReceivedSymbol symbol, out DecoderBlock? block)
    {
        block = null;
        var transferredToBlock = false;
        try
        {
            if (symbol.BlockId < _nextDecodeBlockId || HasDecodedWindowBlockId(symbol.BlockId))
            {
                symbol.Dispose();
                return false;
            }

            if (_hasFinalDecodeBlockId && symbol.BlockId > _finalDecodeBlockId)
            {
                throw new InvalidDataException("Received a block after the final FEC block.");
            }

            if (symbol.SymbolSize != _options.SymbolSize)
            {
                throw new InvalidDataException($"Unexpected symbol size {symbol.SymbolSize}; expected {_options.SymbolSize}.");
            }

            if (symbol.SourceSymbolCount > _options.SourceSymbolsPerBlock)
            {
                throw new InvalidDataException($"Source symbol count {symbol.SourceSymbolCount} exceeds configured limit.");
            }

            var expectedRepairSymbolCount = _options.GetRepairSymbolsForSourceCount(symbol.SourceSymbolCount);
            if (symbol.RepairSymbolCount != expectedRepairSymbolCount)
            {
                throw new InvalidDataException($"Unexpected repair symbol count {symbol.RepairSymbolCount}; expected {expectedRepairSymbolCount}.");
            }

            if (symbol.IsFinalBlock)
            {
                if (_hasFinalDecodeBlockId && symbol.BlockId != _finalDecodeBlockId)
                {
                    throw new InvalidDataException("Received inconsistent final FEC block metadata.");
                }

                _hasFinalDecodeBlockId = true;
                _finalDecodeBlockId = symbol.BlockId;
            }

            if (!_decoderBlocks.TryGetValue(symbol.BlockId, out block))
            {
                if (_decoderBlocks.Count >= _options.MaxDecoderBlocks)
                {
                    throw new InvalidOperationException("Too many incomplete decoder blocks are buffered.");
                }

                block = new DecoderBlock(symbol);
                _decoderBlocks.Add(symbol.BlockId, block);
            }

            transferredToBlock = block.Add(symbol);
            if (!transferredToBlock)
            {
                symbol.Dispose();
                block = null;
                return false;
            }

            return true;
        }
        catch
        {
            if (!transferredToBlock)
            {
                symbol.Dispose();
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            DisposeDecoderBlocks();
        }
    }

    private void EncodePacketCore(
        ReadOnlySpan<byte> rawPacket,
        Span<byte> destination,
        ReadOnlySpan<int> sourceOffsets,
        ReadOnlySpan<int> sourceLengths,
        int repairSymbolCount,
        int requiredSize,
        out int bytesWritten,
        out int packetCount,
        bool isFinalPacket)
    {
        bytesWritten = 0;
        packetCount = 0;
        var sourceCount = sourceLengths.Length;
        var blockId = _nextEncodeBlockId++;
        for (var i = 0; i < sourceCount; i++)
        {
            WriteLengthPrefixedFrame(
                destination,
                ref bytesWritten,
                ref packetCount,
                blockId,
                rawPacket.Length,
                _options.SymbolSize,
                sourceCount,
                repairSymbolCount,
                i,
                isFinalPacket,
                rawPacket.Slice(sourceOffsets[i], sourceLengths[i]));
        }

        var useIntermediateRepair = ShouldUseIntermediateRepairPath(sourceCount, repairSymbolCount);
        var intermediateSymbols = useIntermediateRepair
            ? BuildIntermediateSymbols(sourceCount, rawPacket, sourceOffsets, sourceLengths, _options.SymbolSize)
            : ReadOnlySpan<byte>.Empty;

        for (var repairIndex = 0; repairIndex < repairSymbolCount; repairIndex++)
        {
            WriteLengthPrefixedRepairFrame(
                destination,
                ref bytesWritten,
                ref packetCount,
                blockId,
                rawPacket.Length,
                _options.SymbolSize,
                sourceCount,
                repairSymbolCount,
                repairIndex,
                isFinalPacket,
                rawPacket,
                sourceOffsets,
                sourceLengths,
                intermediateSymbols);
        }

        if (bytesWritten > requiredSize)
        {
            throw new InvalidOperationException("Packetized encoder size calculation is inconsistent.");
        }

        if (isFinalPacket)
        {
            _rawCompleted = true;
        }
    }

    private void EncodeSingleSourcePacketCore(
        ReadOnlySpan<byte> rawPacket,
        Span<byte> destination,
        out int bytesWritten,
        out int packetCount,
        bool isFinalPacket)
    {
        var blockId = _nextEncodeBlockId++;
        var sourcePayload = GetSingleRecordPayload(rawPacket);
        var sourceFrameLength = LinkerFecEncodedSymbol.HeaderSize + sourcePayload.Length;
        WriteFrameLength(destination, 0, sourceFrameLength);

        var sourceFrameOffset = FrameLengthPrefixSize;
        var sourceFrame = destination.Slice(sourceFrameOffset, sourceFrameLength);
        sourcePayload.CopyTo(sourceFrame.Slice(LinkerFecEncodedSymbol.HeaderSize, sourcePayload.Length));

        LinkerFecEncodedSymbol.WriteHeaderUnchecked(
            sourceFrame,
            blockId,
            blockLength: (uint)rawPacket.Length,
            sourceSymbolCount: 1,
            repairSymbolCount: 1,
            symbolId: 0,
            isFinalBlock: isFinalPacket,
            payloadLength: (uint)sourcePayload.Length);

        var repairPayloadLength = sourcePayload.Length;
        var repairFrameLength = LinkerFecEncodedSymbol.HeaderSize +
            LinkerFecEncodedSymbol.RepairLengthSymbolSize +
            repairPayloadLength;
        var repairLengthPrefixOffset = sourceFrameOffset + sourceFrameLength;
        WriteFrameLength(destination, repairLengthPrefixOffset, repairFrameLength);

        var repairFrameOffset = repairLengthPrefixOffset + FrameLengthPrefixSize;
        var repairFrame = destination.Slice(repairFrameOffset, repairFrameLength);
        var repairPayload = repairFrame.Slice(
            LinkerFecEncodedSymbol.HeaderSize + LinkerFecEncodedSymbol.RepairLengthSymbolSize,
            repairPayloadLength);
        var coefficient = GetSingleSourceRepairCoefficient();
        SymbolOperations.ScaleToPadded(repairPayload, sourcePayload, coefficient);
        var repairLengthSymbol = EncodeLengthSymbol((ushort)sourcePayload.Length, coefficient);
        BinaryPrimitives.WriteUInt16LittleEndian(
            repairFrame.Slice(LinkerFecEncodedSymbol.HeaderSize, LinkerFecEncodedSymbol.RepairLengthSymbolSize),
            repairLengthSymbol);

        LinkerFecEncodedSymbol.WriteHeaderUnchecked(
            repairFrame,
            blockId,
            blockLength: (uint)rawPacket.Length,
            sourceSymbolCount: 1,
            repairSymbolCount: 1,
            symbolId: 1,
            isFinalBlock: isFinalPacket,
            payloadLength: (uint)repairPayloadLength);

        bytesWritten = repairFrameOffset + repairFrameLength;
        packetCount = 2;

        if (isFinalPacket)
        {
            _rawCompleted = true;
        }
    }

    private void GenerateRepairSymbol(
        int sourceCount,
        ReadOnlySpan<byte> rawPacket,
        ReadOnlySpan<int> sourceOffsets,
        ReadOnlySpan<int> sourceLengths,
        int symbolSize,
        ReadOnlySpan<byte> coefficients,
        Span<byte> destination)
    {
        var hasOutput = false;
        for (var source = 0; source < sourceCount; source++)
        {
            var coefficient = coefficients[source];
            if (coefficient == 0)
            {
                continue;
            }

            var sourceOffset = sourceOffsets[source];
            var payloadLength = sourceLengths[source];
            if (payloadLength == 0)
            {
                continue;
            }

            var sourceSymbol = rawPacket.Slice(sourceOffset, payloadLength);
            if (hasOutput)
            {
                SymbolOperations.AddScaledPadded(destination, sourceSymbol, coefficient);
            }
            else
            {
                SymbolOperations.ScaleToPadded(destination, sourceSymbol, coefficient);
                hasOutput = true;
            }
        }

        if (!hasOutput)
        {
            destination.Clear();
        }
    }

    private byte[] GetRepairCoefficients(int sourceCount, int repairIndex)
    {
        if (_repairCoefficientCache is null || _repairCoefficientSourceCount != sourceCount)
        {
            _repairCoefficientCache = new byte[_options.MaxRepairSymbolsPerEncodedBlock][];
            _repairCoefficientSourceCount = sourceCount;
        }

        return _repairCoefficientCache[repairIndex] ??=
            FecAlgorithm.GetSourceCoefficientsForEncodingSymbol(sourceCount, sourceCount + repairIndex);
    }

    private byte GetSingleSourceRepairCoefficient()
    {
        if (!_hasSingleSourceRepairCoefficient)
        {
            _singleSourceRepairCoefficient = GetRepairCoefficients(sourceCount: 1, repairIndex: 0)[0];
            _hasSingleSourceRepairCoefficient = true;
        }

        return _singleSourceRepairCoefficient;
    }

    private static void WriteLengthPrefixedFrame(
        Span<byte> destination,
        ref int bytesWritten,
        ref int packetCount,
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int symbolId,
        bool isFinalBlock,
        ReadOnlySpan<byte> payload)
    {
        var frameLength = LinkerFecEncodedSymbol.HeaderSize + payload.Length;
        WriteFrameLength(destination, bytesWritten, frameLength);
        bytesWritten += FrameLengthPrefixSize;

        LinkerFecEncodedSymbol.WriteFrame(
            destination.Slice(bytesWritten, frameLength),
            blockId,
            blockLength,
            symbolSize,
            sourceSymbolCount,
            repairSymbolCount,
            symbolId,
            isFinalBlock,
            payload);

        bytesWritten += frameLength;
        packetCount++;
    }

    private void WriteLengthPrefixedRepairFrame(
        Span<byte> destination,
        ref int bytesWritten,
        ref int packetCount,
        ulong blockId,
        int blockLength,
        int symbolSize,
        int sourceSymbolCount,
        int repairSymbolCount,
        int repairIndex,
        bool isFinalBlock,
        ReadOnlySpan<byte> rawPacket,
        ReadOnlySpan<int> sourceOffsets,
        ReadOnlySpan<int> sourceLengths,
        ReadOnlySpan<byte> intermediateSymbols)
    {
        var coefficients = GetRepairCoefficients(sourceSymbolCount, repairIndex);
        var repairPayloadLength = GetRepairPayloadLength(sourceLengths, coefficients);
        var frameLength = LinkerFecEncodedSymbol.HeaderSize +
            LinkerFecEncodedSymbol.RepairLengthSymbolSize +
            repairPayloadLength;
        WriteFrameLength(destination, bytesWritten, frameLength);
        bytesWritten += FrameLengthPrefixSize;

        var frame = destination.Slice(bytesWritten, frameLength);
        var lengthSymbol = GenerateRepairLengthSymbol(sourceLengths, coefficients);
        BinaryPrimitives.WriteUInt16LittleEndian(
            frame.Slice(LinkerFecEncodedSymbol.HeaderSize, LinkerFecEncodedSymbol.RepairLengthSymbolSize),
            lengthSymbol);

        var payload = frame.Slice(
            LinkerFecEncodedSymbol.HeaderSize + LinkerFecEncodedSymbol.RepairLengthSymbolSize,
            repairPayloadLength);
        var symbolId = sourceSymbolCount + repairIndex;
        if (intermediateSymbols.IsEmpty)
        {
            GenerateRepairSymbol(sourceSymbolCount, rawPacket, sourceOffsets, sourceLengths, symbolSize, coefficients, payload);
        }
        else
        {
            FecAlgorithm.GenerateEncodingSymbol(
                sourceSymbolCount,
                intermediateSymbols,
                symbolSize,
                symbolId,
                payload);
        }

        LinkerFecEncodedSymbol.WriteHeader(
            frame,
            blockId,
            blockLength,
            symbolSize,
            sourceSymbolCount,
            repairSymbolCount,
            symbolId,
            isFinalBlock,
            repairPayloadLength);

        bytesWritten += frameLength;
        packetCount++;
    }

    private bool ShouldUseIntermediateRepairPath(int sourceCount, int repairSymbolCount)
    {
        if (sourceCount <= 1)
        {
            return false;
        }

        return _options.RepairGenerationMode switch
        {
            LinkerFecRepairGenerationMode.SourceCoefficients => false,
            LinkerFecRepairGenerationMode.IntermediateSymbols => true,
            _ => sourceCount >= 64 && repairSymbolCount >= 8
        };
    }

    private ReadOnlySpan<byte> BuildIntermediateSymbols(
        int sourceCount,
        ReadOnlySpan<byte> rawPacket,
        ReadOnlySpan<int> sourceOffsets,
        ReadOnlySpan<int> sourceLengths,
        int symbolSize)
    {
        var parameters = FecParameters.ForSourceSymbolCount(sourceCount);
        var requiredLength = checked(parameters.L * symbolSize);
        var buffer = _intermediateSymbolBuffer;
        if (buffer is null ||
            buffer.Length < requiredLength ||
            _intermediateSymbolBufferSourceCount != sourceCount ||
            _intermediateSymbolBufferSymbolSize != symbolSize)
        {
            buffer = new byte[requiredLength];
            _intermediateSymbolBuffer = buffer;
            _intermediateSymbolBufferSourceCount = sourceCount;
            _intermediateSymbolBufferSymbolSize = symbolSize;
        }

        var symbols = buffer.AsSpan(0, requiredLength);
        FecAlgorithm.GenerateIntermediateSymbols(sourceCount, rawPacket, sourceOffsets, sourceLengths, symbolSize, symbols);
        return symbols;
    }

    private int GetPacketizedOutputSize(ReadOnlySpan<int> sourceLengths, int repairSymbolCount)
    {
        var sourceCount = sourceLengths.Length;
        var size = checked((sourceCount + repairSymbolCount) * (FrameLengthPrefixSize + LinkerFecEncodedSymbol.HeaderSize));
        for (var i = 0; i < sourceLengths.Length; i++)
        {
            size = checked(size + sourceLengths[i]);
        }

        for (var repairIndex = 0; repairIndex < repairSymbolCount; repairIndex++)
        {
            size = checked(size +
                LinkerFecEncodedSymbol.RepairLengthSymbolSize +
                GetRepairPayloadLength(sourceLengths, GetRepairCoefficients(sourceCount, repairIndex)));
        }

        return size;
    }

    private static int GetSingleSourcePacketizedOutputSize(int sourcePayloadLength)
    {
        return checked(
            (2 * sourcePayloadLength) +
            (2 * (FrameLengthPrefixSize + LinkerFecEncodedSymbol.HeaderSize)) +
            LinkerFecEncodedSymbol.RepairLengthSymbolSize);
    }

    private static int GetRepairPayloadLength(ReadOnlySpan<int> sourceLengths, ReadOnlySpan<byte> coefficients)
    {
        var payloadLength = 0;
        for (var source = 0; source < sourceLengths.Length; source++)
        {
            if (coefficients[source] == 0)
            {
                continue;
            }

            payloadLength = Math.Max(payloadLength, sourceLengths[source]);
        }

        return payloadLength;
    }

    private static ushort GenerateRepairLengthSymbol(ReadOnlySpan<int> sourceLengths, ReadOnlySpan<byte> coefficients)
    {
        Span<byte> encoded = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        var hasOutput = false;
        for (var source = 0; source < sourceLengths.Length; source++)
        {
            var coefficient = coefficients[source];
            if (coefficient == 0)
            {
                continue;
            }

            var sourceLength = checked((ushort)sourceLengths[source]);
            if (hasOutput)
            {
                AddScaledLengthSymbol(encoded, sourceLength, coefficient);
            }
            else
            {
                WriteEncodedLengthSymbol(encoded, sourceLength, coefficient);
                hasOutput = true;
            }
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(encoded);
    }

    private static ushort EncodeLengthSymbol(ushort sourceLength, byte coefficient)
    {
        Span<byte> encoded = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        WriteEncodedLengthSymbol(encoded, sourceLength, coefficient);
        return BinaryPrimitives.ReadUInt16LittleEndian(encoded);
    }

    private static void WriteEncodedLengthSymbol(Span<byte> destination, ushort sourceLength, byte coefficient)
    {
        Span<byte> source = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        BinaryPrimitives.WriteUInt16LittleEndian(source, sourceLength);
        SymbolOperations.ScaleTo(destination, source, coefficient);
    }

    private static void AddScaledLengthSymbol(Span<byte> destination, ushort sourceLength, byte coefficient)
    {
        Span<byte> source = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        BinaryPrimitives.WriteUInt16LittleEndian(source, sourceLength);
        SymbolOperations.AddScaled(destination, source, coefficient);
    }

    private static int DecodeSingleSourceLength(ushort lengthSymbol, byte coefficient)
    {
        Span<byte> encoded = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        Span<byte> decoded = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        BinaryPrimitives.WriteUInt16LittleEndian(encoded, lengthSymbol);
        if (coefficient == 1)
        {
            encoded.CopyTo(decoded);
        }
        else
        {
            SymbolOperations.ScaleTo(decoded, encoded, GaloisField256.Inverse(coefficient));
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(decoded);
    }

    private static ReadOnlySpan<byte> GetSingleRecordPayload(ReadOnlySpan<byte> record)
    {
        var packetLength = ReadRecordLength(record);
        return record.Slice(RecordLengthPrefixSize, packetLength);
    }

    private static int BuildSourceSegments(
        ReadOnlySpan<byte> records,
        int symbolSize,
        Span<int> sourceOffsets,
        Span<int> sourceLengths)
    {
        var offset = 0;
        var sourceCount = 0;
        while (offset < records.Length)
        {
            if (records.Length - offset < RecordLengthPrefixSize)
            {
                throw new ArgumentException("Record list ended inside a packet length prefix.", nameof(records));
            }

            var packetLength = ReadRecordLength(records, offset);
            var recordLength = checked(RecordLengthPrefixSize + packetLength);
            if (recordLength > records.Length - offset)
            {
                throw new ArgumentException("Packet length prefix exceeds the remaining payload bytes.", nameof(records));
            }

            if (packetLength > symbolSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(records),
                    packetLength,
                    "Each application payload must fit in one FEC source symbol.");
            }

            if (sourceCount == sourceOffsets.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(records),
                    sourceCount + 1,
                    "Application record count exceeds the configured source symbol limit.");
            }

            sourceOffsets[sourceCount] = offset + RecordLengthPrefixSize;
            sourceLengths[sourceCount] = packetLength;
            sourceCount++;
            offset += recordLength;
        }

        return sourceCount;
    }

    private static void ValidateApplicationRecords(ReadOnlySpan<byte> records, string paramName)
    {
        if (!TryValidateApplicationRecords(records, out var error))
        {
            throw new ArgumentException(error, paramName);
        }
    }

    private static int ValidateDecodedApplicationRecords(ReadOnlySpan<byte> records)
    {
        if (!TryValidateApplicationRecords(records, out var error, out var packetCount))
        {
            throw new InvalidDataException($"Decoded FEC payload is not a valid application record list: {error}");
        }

        return packetCount;
    }

    private static void WriteDecodedPacketKinds(
        Span<LinkerFecDecodedPacketKind> packetKinds,
        bool writePacketKinds,
        int packetCount,
        int sourcePacketCount,
        int recoveredPacketCount)
    {
        if (!writePacketKinds)
        {
            return;
        }

        if (sourcePacketCount < 0 || recoveredPacketCount < 0 || sourcePacketCount + recoveredPacketCount != packetCount)
        {
            throw new InvalidOperationException("Decoded packet kind count is inconsistent with decoded packet count.");
        }

        if (packetKinds.Length < packetCount)
        {
            throw new ArgumentException("Packet kind buffer is smaller than the decoded packet count.", nameof(packetKinds));
        }

        packetKinds[..sourcePacketCount].Fill(LinkerFecDecodedPacketKind.Source);
        packetKinds.Slice(sourcePacketCount, recoveredPacketCount).Fill(LinkerFecDecodedPacketKind.Recovered);
    }

    private static bool TryValidateApplicationRecords(ReadOnlySpan<byte> records, out string error)
    {
        return TryValidateApplicationRecords(records, out error, out _);
    }

    private static bool TryValidateApplicationRecords(ReadOnlySpan<byte> records, out string error, out int packetCount)
    {
        error = string.Empty;
        packetCount = 0;
        if (records.Length < RecordLengthPrefixSize)
        {
            error = "Expected at least one [2-byte length][payload] record.";
            return false;
        }

        var offset = 0;
        while (offset < records.Length)
        {
            if (records.Length - offset < RecordLengthPrefixSize)
            {
                error = "Record list ended inside a packet length prefix.";
                return false;
            }

            var packetLength = ReadRecordLength(records, offset);
            offset += RecordLengthPrefixSize;
            if (packetLength > records.Length - offset)
            {
                error = "Packet length prefix exceeds the remaining payload bytes.";
                return false;
            }

            offset += packetLength;
            packetCount++;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadRecordLength(ReadOnlySpan<byte> records, int offset = 0)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(records.Slice(offset, RecordLengthPrefixSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteRecordLength(Span<byte> destination, int offset, int value)
    {
        if ((uint)value > LinkerFecOptions.MaxRecordPayloadLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Record payload length exceeds the 2-byte record prefix limit.");
        }

        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, RecordLengthPrefixSize), checked((ushort)value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFrameLength(Span<byte> destination, int offset, int value)
    {
        if ((uint)value > LinkerFecOptions.MaxFrameLength)
        {
            throw new InvalidOperationException("FEC frame length exceeds the 2-byte packetized frame prefix limit.");
        }

        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, FrameLengthPrefixSize), checked((ushort)value));
    }

    private bool TryDecodeReceivedBlock(
        ulong blockId,
        Span<byte> destination,
        out int bytesWritten,
        out int fecRecoveredPacketCount)
    {
        bytesWritten = 0;
        fecRecoveredPacketCount = 0;
        if (!_decoderBlocks.TryGetValue(blockId, out var block))
        {
            return false;
        }

        if (!block.CanDecode)
        {
            return false;
        }

        var missingSourceCount = block.MissingSourceCount;
        if (!block.TryDecodeMissing(destination, out bytesWritten))
        {
            return false;
        }

        fecRecoveredPacketCount = missingSourceCount;
        CompleteDecodedBlock(blockId, block);

        return true;
    }

    private void CompleteDecodedBlock(ulong blockId, DecoderBlock block)
    {
        _decoderBlocks.Remove(blockId);
        MarkDecodedWindowBlockId(blockId);
        block.Dispose();
    }

    private static int WriteSourceSymbolRecord(ReceivedSymbol symbol, Span<byte> destination)
    {
        var payload = symbol.PayloadSpan;
        return WriteSourcePayloadRecord(payload, destination);
    }

    private static int WriteSourcePayloadRecord(ReadOnlySpan<byte> payload, Span<byte> destination)
    {
        var recordLength = checked(RecordLengthPrefixSize + payload.Length);
        if (destination.Length < recordLength)
        {
            throw new ArgumentException("Destination buffer is smaller than the decoded source packet.", nameof(destination));
        }

        WriteRecordLength(destination, 0, payload.Length);
        payload.CopyTo(destination.Slice(RecordLengthPrefixSize, payload.Length));
        return recordLength;
    }

    private bool TryDecodeSingleSourceFrame(
        ReadOnlySpan<byte> encodedFrame,
        ulong blockId,
        Span<byte> destination,
        out int bytesWritten,
        out int fecRecoveredPacketCount,
        out bool handled)
    {
        bytesWritten = 0;
        fecRecoveredPacketCount = 0;
        handled = false;

        var symbolSize = _options.SymbolSize;
        var sourceSymbolCount = LinkerFecEncodedSymbol.ReadSourceSymbolCount(encodedFrame);
        if (sourceSymbolCount != 1)
        {
            return false;
        }

        handled = true;

        var repairSymbolCount = _options.GetRepairSymbolsForSourceCount(sourceSymbolCount);
        var symbolId = LinkerFecEncodedSymbol.ReadSymbolId(encodedFrame);
        var payloadLength = LinkerFecEncodedSymbol.ReadPayloadLength(encodedFrame);
        var flags = LinkerFecEncodedSymbol.ReadFlags(encodedFrame);
        var isFinalBlock = (flags & 1) != 0;
        var isRepair = symbolId >= sourceSymbolCount;

        if ((flags & 2) != 0 != isRepair)
        {
            throw new FormatException("Repair flag does not match the symbol id.");
        }

        if (symbolId < 0 || symbolId >= sourceSymbolCount + repairSymbolCount)
        {
            throw new InvalidDataException("Symbol id is outside the block.");
        }

        if (payloadLength > symbolSize)
        {
            throw new InvalidDataException("Payload cannot exceed symbol size.");
        }

        if (isFinalBlock)
        {
            if (_hasFinalDecodeBlockId && blockId != _finalDecodeBlockId)
            {
                throw new InvalidDataException("Received inconsistent final FEC block metadata.");
            }

            _hasFinalDecodeBlockId = true;
            _finalDecodeBlockId = blockId;
        }

        var payload = encodedFrame.Slice(LinkerFecEncodedSymbol.GetPayloadOffset(encodedFrame), payloadLength);

        if (!isRepair)
        {
            var sourceRecordLength = checked(RecordLengthPrefixSize + payloadLength);
            if (destination.Length < sourceRecordLength)
            {
                throw new ArgumentException("Destination buffer is smaller than the decoded source packet.", nameof(destination));
            }

            WriteRecordLength(destination, 0, payloadLength);
            payload.CopyTo(destination.Slice(RecordLengthPrefixSize, payloadLength));

            DisposeDecoderBlock(blockId);
            MarkDecodedWindowBlockId(blockId);
            bytesWritten = sourceRecordLength;
            return true;
        }

        if (payloadLength > symbolSize)
        {
            throw new InvalidDataException("Single-source repair payload length exceeds symbol size.");
        }

        var repairIndex = symbolId - sourceSymbolCount;
        var coefficient = sourceSymbolCount == 1 && repairIndex == 0
            ? GetSingleSourceRepairCoefficient()
            : GetRepairCoefficients(sourceSymbolCount, repairIndex)[0];
        if (coefficient == 0)
        {
            return false;
        }

        var recoveredPayloadLength = DecodeSingleSourceLength(LinkerFecEncodedSymbol.ReadLengthSymbol(encodedFrame), coefficient);
        if (recoveredPayloadLength > payloadLength)
        {
            throw new InvalidDataException("Recovered source payload length is inconsistent with the FEC block.");
        }

        var recoveredRecordLength = checked(RecordLengthPrefixSize + recoveredPayloadLength);
        if (destination.Length < recoveredRecordLength)
        {
            throw new ArgumentException("Destination buffer is smaller than the decoded source packet.", nameof(destination));
        }

        WriteRecordLength(destination, 0, recoveredPayloadLength);
        var recoveredPayload = destination.Slice(RecordLengthPrefixSize, recoveredPayloadLength);
        if (coefficient == 1)
        {
            payload[..recoveredPayloadLength].CopyTo(recoveredPayload);
        }
        else
        {
            SymbolOperations.ScaleTo(recoveredPayload, payload[..recoveredPayloadLength], GaloisField256.Inverse(coefficient));
        }

        DisposeDecoderBlock(blockId);
        MarkDecodedWindowBlockId(blockId);
        bytesWritten = recoveredRecordLength;
        fecRecoveredPacketCount = 1;
        return true;
    }

    private void AdvanceDecodeWindow(ulong blockId)
    {
        if (!_hasDecodeBlockIdSeen || blockId > _maxDecodeBlockIdSeen)
        {
            _hasDecodeBlockIdSeen = true;
            _maxDecodeBlockIdSeen = blockId;
        }

        var skipBlocks = (ulong)_options.MaxSkipBlocks;
        var minBlockId = _maxDecodeBlockIdSeen > skipBlocks
            ? _maxDecodeBlockIdSeen - skipBlocks
            : 0;

        if (minBlockId <= _nextDecodeBlockId)
        {
            return;
        }

        _nextDecodeBlockId = minBlockId;
        DisposeDecoderBlocksBefore(minBlockId);
        RemoveDecodedWindowBlockIdsBefore(minBlockId);
    }

    private ulong ExpandDecodeBlockId(uint blockSequence)
    {
        if (!_hasDecodeBlockIdSeen)
        {
            return blockSequence;
        }

        const ulong blockIdModulo = 1UL << 32;
        const ulong halfModulo = 1UL << 31;

        var epoch = _maxDecodeBlockIdSeen & 0xFFFF_FFFF_0000_0000UL;
        var candidate = epoch | blockSequence;
        if (candidate + halfModulo < _maxDecodeBlockIdSeen)
        {
            return candidate + blockIdModulo;
        }

        if (candidate > _maxDecodeBlockIdSeen + halfModulo && candidate >= blockIdModulo)
        {
            return candidate - blockIdModulo;
        }

        return candidate;
    }

    private void MarkDecodedWindowBlockId(ulong blockId)
    {
        if (blockId == _nextDecodeBlockId)
        {
            _nextDecodeBlockId++;
            while (RemoveDecodedWindowBlockId(_nextDecodeBlockId))
            {
                _nextDecodeBlockId++;
            }

            DisposeDecoderBlocksBefore(_nextDecodeBlockId);
            RemoveDecodedWindowBlockIdsBefore(_nextDecodeBlockId);
            return;
        }

        AddDecodedWindowBlockId(blockId);
        while (RemoveDecodedWindowBlockId(_nextDecodeBlockId))
        {
            _nextDecodeBlockId++;
        }

        DisposeDecoderBlocksBefore(_nextDecodeBlockId);
        RemoveDecodedWindowBlockIdsBefore(_nextDecodeBlockId);
    }

    private bool HasDecodedWindowBlockId(ulong blockId)
    {
        var blockIds = _decodedWindowBlockIds;
        if (blockIds is null)
        {
            return false;
        }

        for (var i = 0; i < _decodedWindowBlockIdCount; i++)
        {
            if (blockIds[i] == blockId)
            {
                return true;
            }
        }

        return false;
    }

    private void DisposeDecoderBlock(ulong blockId)
    {
        if (_decoderBlocks.Remove(blockId, out var block))
        {
            block.Dispose();
        }

    }

    private void AddDecodedWindowBlockId(ulong blockId)
    {
        if (HasDecodedWindowBlockId(blockId))
        {
            return;
        }

        var blockIds = _decodedWindowBlockIds;
        if (blockIds is null)
        {
            blockIds = new ulong[Math.Min(_options.MaxSkipBlocks + 1, 16)];
            _decodedWindowBlockIds = blockIds;
        }
        else if (_decodedWindowBlockIdCount == blockIds.Length)
        {
            var nextLength = Math.Min(checked(blockIds.Length * 2), _options.MaxSkipBlocks + 1);
            if (nextLength == blockIds.Length)
            {
                RemoveDecodedWindowBlockIdsBefore(_nextDecodeBlockId);
                if (_decodedWindowBlockIdCount == blockIds.Length)
                {
                    throw new InvalidOperationException("Decoded block window is full.");
                }

                blockIds = _decodedWindowBlockIds;
            }
            else
            {
                Array.Resize(ref blockIds, nextLength);
                _decodedWindowBlockIds = blockIds;
            }
        }

        blockIds![_decodedWindowBlockIdCount++] = blockId;
    }

    private bool RemoveDecodedWindowBlockId(ulong blockId)
    {
        var blockIds = _decodedWindowBlockIds;
        if (blockIds is null)
        {
            return false;
        }

        for (var i = 0; i < _decodedWindowBlockIdCount; i++)
        {
            if (blockIds[i] != blockId)
            {
                continue;
            }

            _decodedWindowBlockIdCount--;
            blockIds[i] = blockIds[_decodedWindowBlockIdCount];
            blockIds[_decodedWindowBlockIdCount] = 0;
            return true;
        }

        return false;
    }

    private void RemoveDecodedWindowBlockIdsBefore(ulong minBlockId)
    {
        var blockIds = _decodedWindowBlockIds;
        if (blockIds is null)
        {
            return;
        }

        var writeIndex = 0;
        for (var readIndex = 0; readIndex < _decodedWindowBlockIdCount; readIndex++)
        {
            var blockId = blockIds[readIndex];
            if (blockId >= minBlockId)
            {
                blockIds[writeIndex++] = blockId;
            }
        }

        blockIds.AsSpan(writeIndex, _decodedWindowBlockIdCount - writeIndex).Clear();
        _decodedWindowBlockIdCount = writeIndex;
    }

    private void DisposeDecoderBlocksBefore(ulong minBlockId)
    {
        while (TryFindDecoderBlockBefore(minBlockId, out var blockId))
        {
            var block = _decoderBlocks[blockId];
            _decoderBlocks.Remove(blockId);
            block.Dispose();
        }

    }

    private bool TryFindDecoderBlockBefore(ulong minBlockId, out ulong blockId)
    {
        blockId = 0;
        foreach (var pair in _decoderBlocks)
        {
            if (pair.Key < minBlockId)
            {
                blockId = pair.Key;
                return true;
            }
        }

        return false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LinkerFecCodec));
        }
    }

    private void DisposeDecoderBlocks()
    {
        foreach (var block in _decoderBlocks.Values)
        {
            block.Dispose();
        }

        _decoderBlocks.Clear();
    }
}
