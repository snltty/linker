using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace linker.fec.Internal;

internal sealed class DecoderBlock : IDisposable
{
    private const int StackAllocByteThreshold = 64 * 1024;
    private const int StackAllocIntThreshold = 1024;

    private ReceivedSymbol[] _sourceSymbols;
    private ReceivedSymbol[] _repairSymbols;
    private int _sourceReceivedCount;
    private int _repairReceivedCount;
    private bool _disposed;

    public DecoderBlock(ReceivedSymbol firstSymbol)
    {
        BlockId = firstSymbol.BlockId;
        BlockLength = firstSymbol.BlockLength;
        SymbolSize = firstSymbol.SymbolSize;
        SourceSymbolCount = firstSymbol.SourceSymbolCount;
        RepairSymbolCount = firstSymbol.RepairSymbolCount;
        IsFinalBlock = firstSymbol.IsFinalBlock;

        _sourceSymbols = ArrayPool<ReceivedSymbol>.Shared.Rent(SourceSymbolCount);
        _repairSymbols = ArrayPool<ReceivedSymbol>.Shared.Rent(RepairSymbolCount);
        _sourceSymbols.AsSpan(0, SourceSymbolCount).Clear();
        _repairSymbols.AsSpan(0, RepairSymbolCount).Clear();
    }

    public ulong BlockId { get; }

    public int BlockLength { get; }

    public int SymbolSize { get; }

    public int SourceSymbolCount { get; }

    public int RepairSymbolCount { get; }

    public bool IsFinalBlock { get; }

    public bool CanDecode
    {
        get
        {
            var missingCount = CountMissingSourceSymbols();
            return missingCount == 0 || CountRepairSymbols() >= missingCount;
        }
    }

    public int MissingSourceCount => CountMissingSourceSymbols();

    public bool Add(ReceivedSymbol symbol)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (symbol.BlockId != BlockId ||
            symbol.BlockLength != BlockLength ||
            symbol.SymbolSize != SymbolSize ||
            symbol.SourceSymbolCount != SourceSymbolCount ||
            symbol.RepairSymbolCount != RepairSymbolCount ||
            symbol.IsFinalBlock != IsFinalBlock)
        {
            throw new InvalidDataException("FEC symbol metadata is inconsistent within a block.");
        }

        if (symbol.IsRepair)
        {
            var repairIndex = symbol.SymbolId - SourceSymbolCount;
            if (_repairSymbols[repairIndex].IsAssigned)
            {
                return false;
            }

            _repairSymbols[repairIndex] = symbol;
            _repairReceivedCount++;
            return true;
        }

        if (_sourceSymbols[symbol.SymbolId].IsAssigned)
        {
            return false;
        }

        _sourceSymbols[symbol.SymbolId] = symbol;
        _sourceReceivedCount++;
        return true;
    }

    public bool TryDecode(Span<byte> destination, out int bytesWritten)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        bytesWritten = 0;
        if (destination.Length < BlockLength)
        {
            throw new ArgumentException("Destination buffer is smaller than the decoded block.", nameof(destination));
        }

        if (TryDecodeSystematic(destination, out bytesWritten))
        {
            return true;
        }

        var missingCount = CountMissingSourceSymbols();
        if (CountRepairSymbols() < missingCount)
        {
            return false;
        }

        return TryDecodeSourceSystem(destination, missingCount, includeKnownSources: true, out bytesWritten);
    }

    public bool TryDecodeMissing(Span<byte> destination, out int bytesWritten)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        bytesWritten = 0;
        var missingCount = CountMissingSourceSymbols();
        if (missingCount == 0 || CountRepairSymbols() < missingCount)
        {
            return false;
        }

        return TryDecodeSourceSystem(destination, missingCount, includeKnownSources: false, out bytesWritten);
    }

    private int CountMissingSourceSymbols()
    {
        return SourceSymbolCount - _sourceReceivedCount;
    }

    private int CountRepairSymbols()
    {
        return _repairReceivedCount;
    }

    private bool TryDecodeSourceSystem(
        Span<byte> destination,
        int missingCount,
        bool includeKnownSources,
        out int bytesWritten)
    {
        bytesWritten = 0;
        if (missingCount == 0)
        {
            return CopySystematicTo(destination, out bytesWritten);
        }

        int[]? rentedMissing = null;
        Span<int> missingIds = SourceSymbolCount <= StackAllocIntThreshold
            ? stackalloc int[SourceSymbolCount]
            : rentedMissing = ArrayPool<int>.Shared.Rent(SourceSymbolCount);

        try
        {
            var actualMissingCount = FillMissingIds(missingIds);
            missingIds = missingIds[..actualMissingCount];
            if (actualMissingCount != missingCount)
            {
                throw new InvalidOperationException("Missing source count changed during decode.");
            }

            if (missingIds.Length == 1)
            {
                return TryRecoverSingleSource(destination, missingIds[0], includeKnownSources, out bytesWritten);
            }

            return TryRecoverMultipleSources(destination, missingIds, includeKnownSources, out bytesWritten);
        }
        finally
        {
            if (rentedMissing is not null)
            {
                ArrayPool<int>.Shared.Return(rentedMissing);
            }
        }
    }

    private bool TryDecodeSystematic(Span<byte> destination, out int bytesWritten)
    {
        if (_sourceReceivedCount != SourceSymbolCount)
        {
            bytesWritten = 0;
            return false;
        }

        return CopySystematicTo(destination, out bytesWritten);
    }

    private bool CopySystematicTo(Span<byte> destination, out int bytesWritten)
    {
        var writeOffset = 0;
        for (var i = 0; i < SourceSymbolCount; i++)
        {
            CopySourcePayloadTo(_sourceSymbols[i].PayloadSpan, destination, ref writeOffset);
        }

        bytesWritten = writeOffset;
        return true;
    }

    private int FillMissingIds(Span<int> missingIds)
    {
        var missingCount = 0;
        for (var sourceId = 0; sourceId < SourceSymbolCount; sourceId++)
        {
            if (!_sourceSymbols[sourceId].IsAssigned)
            {
                missingIds[missingCount++] = sourceId;
            }
        }

        return missingCount;
    }

    private bool TryRecoverSingleSource(
        Span<byte> destination,
        int missingSourceId,
        bool includeKnownSources,
        out int bytesWritten)
    {
        byte[]? rentedRecovered = null;
        Span<byte> recovered = SymbolSize <= StackAllocByteThreshold
            ? stackalloc byte[SymbolSize]
            : rentedRecovered = ArrayPool<byte>.Shared.Rent(SymbolSize);

        try
        {
            for (var repairIndex = 0; repairIndex < RepairSymbolCount; repairIndex++)
            {
                var repair = _repairSymbols[repairIndex];
                if (!repair.IsAssigned)
                {
                    continue;
                }

                var coefficients = FecAlgorithm.GetSourceCoefficientsForEncodingSymbol(SourceSymbolCount, repair.SymbolId);
                var missingCoefficient = coefficients[missingSourceId];
                if (missingCoefficient == 0)
                {
                    continue;
                }

                var recoveredSymbol = recovered[..SymbolSize];
                if (repair.PayloadSpan.Length < recoveredSymbol.Length)
                {
                    recoveredSymbol.Clear();
                }

                repair.PayloadSpan.CopyTo(recoveredSymbol);
                Span<byte> recoveredLength = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
                BinaryPrimitives.WriteUInt16LittleEndian(recoveredLength, repair.LengthSymbol);

                for (var sourceId = 0; sourceId < SourceSymbolCount; sourceId++)
                {
                    var known = _sourceSymbols[sourceId];
                    if (known.IsAssigned)
                    {
                        SymbolOperations.AddScaledPadded(recoveredSymbol, known.PayloadSpan, coefficients[sourceId]);
                        AddScaledLengthSymbol(recoveredLength, known.PayloadSpan.Length, coefficients[sourceId]);
                    }
                }

                if (missingCoefficient != 1)
                {
                    var inverse = GaloisField256.Inverse(missingCoefficient);
                    SymbolOperations.MultiplyInPlace(recoveredSymbol, inverse);
                    SymbolOperations.MultiplyInPlace(recoveredLength, inverse);
                }

                var recoveredPayloadLength = BinaryPrimitives.ReadUInt16LittleEndian(recoveredLength);
                if (includeKnownSources)
                {
                    CopyKnownAndRecoveredTo(destination, missingSourceId, recoveredSymbol, recoveredPayloadLength, out bytesWritten);
                }
                else
                {
                    var writeOffset = 0;
                    CopyRecoveredSourcePayloadTo(recoveredSymbol, recoveredPayloadLength, destination, ref writeOffset);
                    bytesWritten = writeOffset;
                }

                return true;
            }

            bytesWritten = 0;
            return false;
        }
        finally
        {
            if (rentedRecovered is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedRecovered);
            }
        }
    }

    private bool TryRecoverMultipleSources(
        Span<byte> destination,
        Span<int> missingIds,
        bool includeKnownSources,
        out int bytesWritten)
    {
        var repairCount = CountRepairSymbols();
        var matrixLength = checked(repairCount * missingIds.Length);
        var valueSymbolSize = checked(LinkerFecEncodedSymbol.RepairLengthSymbolSize + SymbolSize);
        var valuesLength = checked(repairCount * valueSymbolSize);
        byte[]? rentedMatrix = null;
        byte[]? rentedValues = null;
        int[]? rentedPivots = null;
        int[]? rentedRows = null;

        Span<byte> matrix = matrixLength <= StackAllocByteThreshold
            ? stackalloc byte[matrixLength]
            : rentedMatrix = ArrayPool<byte>.Shared.Rent(matrixLength);
        Span<byte> values = valuesLength <= StackAllocByteThreshold
            ? stackalloc byte[valuesLength]
            : rentedValues = ArrayPool<byte>.Shared.Rent(valuesLength);
        Span<int> pivotColumns = missingIds.Length <= StackAllocIntThreshold
            ? stackalloc int[missingIds.Length]
            : rentedPivots = ArrayPool<int>.Shared.Rent(missingIds.Length);
        Span<int> solutionRows = missingIds.Length <= StackAllocIntThreshold
            ? stackalloc int[missingIds.Length]
            : rentedRows = ArrayPool<int>.Shared.Rent(missingIds.Length);

        matrix = matrix[..matrixLength];
        values = values[..valuesLength];
        pivotColumns = pivotColumns[..missingIds.Length];
        solutionRows = solutionRows[..missingIds.Length];
        matrix.Clear();
        values.Clear();

        try
        {
            var row = 0;
            for (var repairIndex = 0; repairIndex < RepairSymbolCount; repairIndex++)
            {
                var repair = _repairSymbols[repairIndex];
                if (!repair.IsAssigned)
                {
                    continue;
                }

                var coefficients = FecAlgorithm.GetSourceCoefficientsForEncodingSymbol(SourceSymbolCount, repair.SymbolId);
                var value = values.Slice(row * valueSymbolSize, valueSymbolSize);
                BinaryPrimitives.WriteUInt16LittleEndian(value[..LinkerFecEncodedSymbol.RepairLengthSymbolSize], repair.LengthSymbol);
                repair.PayloadSpan.CopyTo(value[LinkerFecEncodedSymbol.RepairLengthSymbolSize..]);

                for (var sourceId = 0; sourceId < SourceSymbolCount; sourceId++)
                {
                    var known = _sourceSymbols[sourceId];
                    if (known.IsAssigned)
                    {
                        AddScaledLengthSymbol(
                            value[..LinkerFecEncodedSymbol.RepairLengthSymbolSize],
                            known.PayloadSpan.Length,
                            coefficients[sourceId]);
                        SymbolOperations.AddScaledPadded(
                            value.Slice(LinkerFecEncodedSymbol.RepairLengthSymbolSize, SymbolSize),
                            known.PayloadSpan,
                            coefficients[sourceId]);
                    }
                }

                for (var i = 0; i < missingIds.Length; i++)
                {
                    matrix[(row * missingIds.Length) + i] = coefficients[missingIds[i]];
                }

                row++;
            }

            if (row != repairCount)
            {
                throw new InvalidOperationException("Repair symbol count changed during decode.");
            }

            if (!LinearSystemSolver.TrySolveInPlace(matrix, repairCount, missingIds.Length, values, valueSymbolSize, pivotColumns))
            {
                bytesWritten = 0;
                return false;
            }

            solutionRows.Fill(-1);
            for (var rowIndex = 0; rowIndex < missingIds.Length; rowIndex++)
            {
                solutionRows[pivotColumns[rowIndex]] = rowIndex;
            }

            if (includeKnownSources)
            {
                CopyKnownAndRecoveredTo(destination, missingIds, values, valueSymbolSize, solutionRows, out bytesWritten);
            }
            else
            {
                CopyRecoveredOnlyTo(destination, missingIds, values, valueSymbolSize, solutionRows, out bytesWritten);
            }

            return true;
        }
        finally
        {
            if (rentedMatrix is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedMatrix);
            }

            if (rentedValues is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedValues);
            }

            if (rentedPivots is not null)
            {
                ArrayPool<int>.Shared.Return(rentedPivots);
            }

            if (rentedRows is not null)
            {
                ArrayPool<int>.Shared.Return(rentedRows);
            }
        }
    }

    private void CopyKnownAndRecoveredTo(
        Span<byte> destination,
        int missingSourceId,
        ReadOnlySpan<byte> recovered,
        int recoveredPayloadLength,
        out int bytesWritten)
    {
        var writeOffset = 0;
        for (var sourceId = 0; sourceId < SourceSymbolCount; sourceId++)
        {
            if (sourceId == missingSourceId)
            {
                CopyRecoveredSourcePayloadTo(recovered, recoveredPayloadLength, destination, ref writeOffset);
            }
            else if (_sourceSymbols[sourceId].IsAssigned)
            {
                var known = _sourceSymbols[sourceId];
                CopySourcePayloadTo(known.PayloadSpan, destination, ref writeOffset);
            }
            else
            {
                throw new InvalidOperationException("A source symbol is missing from the recovered output.");
            }
        }

        bytesWritten = writeOffset;
    }

    private void CopyKnownAndRecoveredTo(
        Span<byte> destination,
        ReadOnlySpan<int> missingIds,
        ReadOnlySpan<byte> values,
        int valueSymbolSize,
        ReadOnlySpan<int> solutionRows,
        out int bytesWritten)
    {
        var missingIndex = 0;
        var writeOffset = 0;
        for (var sourceId = 0; sourceId < SourceSymbolCount; sourceId++)
        {
            if (_sourceSymbols[sourceId].IsAssigned)
            {
                var known = _sourceSymbols[sourceId];
                CopySourcePayloadTo(known.PayloadSpan, destination, ref writeOffset);
                continue;
            }

            if (missingIndex >= missingIds.Length || missingIds[missingIndex] != sourceId)
            {
                throw new InvalidOperationException("Recovered source symbol ordering is inconsistent.");
            }

            var solutionRow = solutionRows[missingIndex];
            if (solutionRow < 0)
            {
                throw new InvalidOperationException("Recovered source symbol is missing from the solved matrix.");
            }

            var value = values.Slice(solutionRow * valueSymbolSize, valueSymbolSize);
            var recoveredPayloadLength = BinaryPrimitives.ReadUInt16LittleEndian(value[..LinkerFecEncodedSymbol.RepairLengthSymbolSize]);
            CopyRecoveredSourcePayloadTo(
                value.Slice(LinkerFecEncodedSymbol.RepairLengthSymbolSize, SymbolSize),
                recoveredPayloadLength,
                destination,
                ref writeOffset);
            missingIndex++;
        }

        bytesWritten = writeOffset;
    }

    private void CopyRecoveredOnlyTo(
        Span<byte> destination,
        ReadOnlySpan<int> missingIds,
        ReadOnlySpan<byte> values,
        int valueSymbolSize,
        ReadOnlySpan<int> solutionRows,
        out int bytesWritten)
    {
        var writeOffset = 0;
        for (var missingIndex = 0; missingIndex < missingIds.Length; missingIndex++)
        {
            var solutionRow = solutionRows[missingIndex];
            if (solutionRow < 0)
            {
                throw new InvalidOperationException("Recovered source symbol is missing from the solved matrix.");
            }

            var value = values.Slice(solutionRow * valueSymbolSize, valueSymbolSize);
            var recoveredPayloadLength = BinaryPrimitives.ReadUInt16LittleEndian(value[..LinkerFecEncodedSymbol.RepairLengthSymbolSize]);
            CopyRecoveredSourcePayloadTo(
                value.Slice(LinkerFecEncodedSymbol.RepairLengthSymbolSize, SymbolSize),
                recoveredPayloadLength,
                destination,
                ref writeOffset);
        }

        bytesWritten = writeOffset;
    }

    private void CopySourcePayloadTo(ReadOnlySpan<byte> payload, Span<byte> destination, ref int writeOffset)
    {
        if (payload.Length > SymbolSize)
        {
            throw new InvalidDataException("Source payload exceeds the configured symbol size.");
        }

        WriteRecordTo(payload, destination, ref writeOffset);
    }

    private void CopyRecoveredSourcePayloadTo(
        ReadOnlySpan<byte> recovered,
        int payloadLength,
        Span<byte> destination,
        ref int writeOffset)
    {
        if (payloadLength < 0 || payloadLength > SymbolSize || payloadLength > recovered.Length)
        {
            throw new InvalidDataException("Recovered source payload length exceeds the source symbol payload.");
        }

        WriteRecordTo(recovered[..payloadLength], destination, ref writeOffset);
    }

    private void WriteRecordTo(ReadOnlySpan<byte> payload, Span<byte> destination, ref int writeOffset)
    {
        var recordLength = checked(LinkerFecOptions.RecordLengthPrefixSize + payload.Length);
        if (recordLength > BlockLength - writeOffset)
        {
            throw new InvalidDataException("Decoded source payloads exceed the block length.");
        }

        BinaryPrimitives.WriteUInt16LittleEndian(
            destination.Slice(writeOffset, LinkerFecOptions.RecordLengthPrefixSize),
            checked((ushort)payload.Length));
        writeOffset += LinkerFecOptions.RecordLengthPrefixSize;
        payload.CopyTo(destination.Slice(writeOffset, payload.Length));
        writeOffset += payload.Length;
    }

    private static void AddScaledLengthSymbol(Span<byte> destination, int sourceLength, byte coefficient)
    {
        Span<byte> source = stackalloc byte[LinkerFecEncodedSymbol.RepairLengthSymbolSize];
        BinaryPrimitives.WriteUInt16LittleEndian(source, checked((ushort)sourceLength));
        SymbolOperations.AddScaled(destination, source, coefficient);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        for (var sourceId = 0; sourceId < SourceSymbolCount; sourceId++)
        {
            if (_sourceSymbols[sourceId].IsAssigned)
            {
                _sourceSymbols[sourceId].Dispose();
                _sourceSymbols[sourceId] = default;
            }
        }

        for (var repairIndex = 0; repairIndex < RepairSymbolCount; repairIndex++)
        {
            if (_repairSymbols[repairIndex].IsAssigned)
            {
                _repairSymbols[repairIndex].Dispose();
                _repairSymbols[repairIndex] = default;
            }
        }

        ArrayPool<ReceivedSymbol>.Shared.Return(_sourceSymbols, clearArray: true);
        ArrayPool<ReceivedSymbol>.Shared.Return(_repairSymbols, clearArray: true);
        _sourceSymbols = Array.Empty<ReceivedSymbol>();
        _repairSymbols = Array.Empty<ReceivedSymbol>();
    }
}
