using System;
using System.Collections.Generic;

namespace linker.fec.Internal;

internal static class FecAlgorithm
{
    private static readonly object PlanGate = new();
    private static readonly Dictionary<int, EncodingPlan> EncodingPlans = [];

    public static byte[][] GenerateIntermediateSymbols(byte[][] sourceSymbols, int symbolSize)
    {
        if (sourceSymbols.Length == 0)
        {
            throw new ArgumentException("At least one source symbol is required.", nameof(sourceSymbols));
        }

        var sourceCount = sourceSymbols.Length;
        for (var x = 0; x < sourceSymbols.Length; x++)
        {
            ValidateSymbol(sourceSymbols[x], symbolSize, nameof(sourceSymbols));
        }

        var plan = GetEncodingPlan(sourceCount);
        var intermediateSymbols = new byte[plan.Parameters.L][];
        for (var i = 0; i < intermediateSymbols.Length; i++)
        {
            intermediateSymbols[i] = new byte[symbolSize];
        }

        GaloisMatrix.MultiplyBySourceRows(
            plan.Inverse,
            plan.Parameters.L,
            plan.Parameters.S + plan.Parameters.H,
            sourceSymbols,
            intermediateSymbols);

        return intermediateSymbols;
    }

    public static void GenerateIntermediateSymbols(
        int sourceCount,
        ReadOnlySpan<byte> rawPacket,
        ReadOnlySpan<int> sourceOffsets,
        ReadOnlySpan<int> sourceLengths,
        int symbolSize,
        Span<byte> destination)
    {
        if (sourceOffsets.Length < sourceCount || sourceLengths.Length < sourceCount)
        {
            throw new ArgumentException("Source segment metadata does not match the source count.");
        }

        var plan = GetEncodingPlan(sourceCount);
        var requiredLength = checked(plan.Parameters.L * symbolSize);
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException("Destination buffer is too small for the intermediate symbols.", nameof(destination));
        }

        var firstSourceRow = plan.Parameters.S + plan.Parameters.H;
        for (var output = 0; output < plan.Parameters.L; output++)
        {
            var outputSymbol = destination.Slice(output * symbolSize, symbolSize);
            var inverseRow = plan.Inverse.AsSpan(output * plan.Parameters.L, plan.Parameters.L);
            var hasOutput = false;

            for (var source = 0; source < sourceCount; source++)
            {
                var coefficient = inverseRow[firstSourceRow + source];
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
                    SymbolOperations.AddScaledPadded(outputSymbol, sourceSymbol, coefficient);
                }
                else
                {
                    SymbolOperations.ScaleToPadded(outputSymbol, sourceSymbol, coefficient);
                    hasOutput = true;
                }
            }

            if (!hasOutput)
            {
                outputSymbol.Clear();
            }
        }
    }

    public static void GenerateEncodingSymbol(int sourceCount, byte[][] intermediateSymbols, int encodingSymbolId, Span<byte> destination)
    {
        var parameters = FecParameters.ForSourceSymbolCount(sourceCount);
        if (intermediateSymbols.Length != parameters.L)
        {
            throw new ArgumentException("Intermediate symbol count does not match FEC parameters.", nameof(intermediateSymbols));
        }

        var internalSymbolId = EncodingSymbolIdToInternalSymbolId(sourceCount, parameters.KPrime, encodingSymbolId);
        Encode(parameters, intermediateSymbols, FecGenerators.Tuple(parameters, internalSymbolId), destination);
    }

    public static void GenerateEncodingSymbol(
        int sourceCount,
        ReadOnlySpan<byte> intermediateSymbols,
        int symbolSize,
        int encodingSymbolId,
        Span<byte> destination)
    {
        var parameters = FecParameters.ForSourceSymbolCount(sourceCount);
        if (intermediateSymbols.Length < checked(parameters.L * symbolSize))
        {
            throw new ArgumentException("Intermediate symbol buffer is too small.", nameof(intermediateSymbols));
        }

        if (destination.Length > symbolSize)
        {
            throw new ArgumentException("Destination cannot be larger than T octets.", nameof(destination));
        }

        var internalSymbolId = EncodingSymbolIdToInternalSymbolId(sourceCount, parameters.KPrime, encodingSymbolId);
        Encode(parameters, intermediateSymbols, symbolSize, FecGenerators.Tuple(parameters, internalSymbolId), destination);
    }

    public static void GenerateRepairSymbol(int sourceCount, byte[][] sourceSymbols, int encodingSymbolId, Span<byte> destination)
    {
        if (encodingSymbolId < sourceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(encodingSymbolId), encodingSymbolId, "Repair symbols must have ESI >= K.");
        }

        var plan = GetEncodingPlan(sourceCount);
        var coefficients = plan.GetSourceCoefficients(encodingSymbolId);
        var hasOutput = false;
        for (var source = 0; source < sourceSymbols.Length; source++)
        {
            var sourceSymbol = sourceSymbols[source];
            ValidateSymbol(sourceSymbol, destination.Length, nameof(sourceSymbols));

            var coefficient = coefficients[source];
            if (coefficient == 0)
            {
                continue;
            }

            if (hasOutput)
            {
                SymbolOperations.AddScaled(destination, sourceSymbol, coefficient);
            }
            else
            {
                SymbolOperations.ScaleTo(destination, sourceSymbol, coefficient);
                hasOutput = true;
            }
        }

        if (!hasOutput)
        {
            destination.Clear();
        }
    }

    public static void GenerateRepairSymbol(int sourceCount, IReadOnlyList<ReadOnlyMemory<byte>> sourceSymbols, int encodingSymbolId, Span<byte> destination)
    {
        if (encodingSymbolId < sourceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(encodingSymbolId), encodingSymbolId, "Repair symbols must have ESI >= K.");
        }

        if (sourceSymbols.Count != sourceCount)
        {
            throw new ArgumentException("Source symbol count does not match K.", nameof(sourceSymbols));
        }

        var plan = GetEncodingPlan(sourceCount);
        var coefficients = plan.GetSourceCoefficients(encodingSymbolId);
        var hasOutput = false;
        for (var source = 0; source < sourceSymbols.Count; source++)
        {
            var symbol = sourceSymbols[source];
            if (symbol.Length != destination.Length)
            {
                throw new ArgumentException("All symbols must have exactly T octets.", nameof(sourceSymbols));
            }

            var coefficient = coefficients[source];
            if (coefficient == 0)
            {
                continue;
            }

            if (hasOutput)
            {
                SymbolOperations.AddScaled(destination, symbol.Span, coefficient);
            }
            else
            {
                SymbolOperations.ScaleTo(destination, symbol.Span, coefficient);
                hasOutput = true;
            }
        }

        if (!hasOutput)
        {
            destination.Clear();
        }
    }

    public static void GenerateRepairSymbol(
        int sourceCount,
        ReadOnlySpan<byte> sourceSymbols,
        int symbolSize,
        int encodingSymbolId,
        Span<byte> destination)
    {
        if (encodingSymbolId < sourceCount)
        {
            throw new ArgumentOutOfRangeException(nameof(encodingSymbolId), encodingSymbolId, "Repair symbols must have ESI >= K.");
        }

        if (symbolSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(symbolSize), symbolSize, "Symbol size must be positive.");
        }

        if (destination.Length != symbolSize)
        {
            throw new ArgumentException("Destination must have exactly T octets.", nameof(destination));
        }

        if (sourceSymbols.Length != checked(sourceCount * symbolSize))
        {
            throw new ArgumentException("Source symbol buffer length does not match K * T.", nameof(sourceSymbols));
        }

        var plan = GetEncodingPlan(sourceCount);
        var coefficients = plan.GetSourceCoefficients(encodingSymbolId);
        var hasOutput = false;
        for (var source = 0; source < sourceCount; source++)
        {
            var coefficient = coefficients[source];
            if (coefficient == 0)
            {
                continue;
            }

            var sourceSymbol = sourceSymbols.Slice(source * symbolSize, symbolSize);
            if (hasOutput)
            {
                SymbolOperations.AddScaled(destination, sourceSymbol, coefficient);
            }
            else
            {
                SymbolOperations.ScaleTo(destination, sourceSymbol, coefficient);
                hasOutput = true;
            }
        }

        if (!hasOutput)
        {
            destination.Clear();
        }
    }

    public static byte[] GetSourceCoefficientsForEncodingSymbol(int sourceCount, int encodingSymbolId)
    {
        var plan = GetEncodingPlan(sourceCount);
        return plan.GetSourceCoefficients(encodingSymbolId);
    }

    public static bool TryDecode(
        int sourceCount,
        int symbolSize,
        IReadOnlyCollection<LinkerFecEncodedSymbol> receivedSymbols,
        out byte[][] sourceSymbols)
    {
        sourceSymbols = [];
        var parameters = FecParameters.ForSourceSymbolCount(sourceCount);
        var paddingSymbolCount = parameters.KPrime - sourceCount;
        var rowCount = parameters.S + parameters.H + paddingSymbolCount + receivedSymbols.Count;
        if (rowCount < parameters.L)
        {
            return false;
        }

        var l = parameters.L;
        var matrix = new byte[checked(rowCount * l)];
        var values = new byte[checked(rowCount * symbolSize)];
        FecMatrix.BuildPrecodeRows(parameters, matrix, rowCount, l);

        var row = parameters.S + parameters.H;
        for (var x = sourceCount; x < parameters.KPrime; x++, row++)
        {
            FecMatrix.BuildEncodingRow(parameters, (uint)x, matrix.AsSpan(row * l, l));
        }

        foreach (var symbol in receivedSymbols)
        {
            var internalSymbolId = EncodingSymbolIdToInternalSymbolId(sourceCount, parameters.KPrime, symbol.SymbolId);
            FecMatrix.BuildEncodingRow(parameters, internalSymbolId, matrix.AsSpan(row * l, l));
            symbol.Payload.Span.CopyTo(values.AsSpan(row * symbolSize, symbol.Payload.Length));
            row++;
        }

        if (!LinearSystemSolver.TrySolve(matrix, rowCount, l, values, symbolSize, out var intermediateSymbols))
        {
            return false;
        }

        sourceSymbols = new byte[sourceCount][];
        for (var x = 0; x < sourceSymbols.Length; x++)
        {
            var source = new byte[symbolSize];
            Encode(parameters, intermediateSymbols, FecGenerators.Tuple(parameters, (uint)x), source);
            sourceSymbols[x] = source;
        }

        return true;
    }

    public static uint EncodingSymbolIdToInternalSymbolId(int sourceCount, int kPrime, int encodingSymbolId)
    {
        if (encodingSymbolId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(encodingSymbolId), encodingSymbolId, "ESI cannot be negative.");
        }

        return encodingSymbolId < sourceCount
            ? (uint)encodingSymbolId
            : checked((uint)(encodingSymbolId + kPrime - sourceCount));
    }

    private static void Encode(FecParameters parameters, byte[][] intermediateSymbols, FecTuple tuple, Span<byte> destination)
    {
        destination.Clear();

        var b = tuple.B;
        SymbolOperations.Xor(destination, intermediateSymbols[b]);
        for (var j = 1; j < tuple.D; j++)
        {
            b = (b + tuple.A) % parameters.W;
            SymbolOperations.Xor(destination, intermediateSymbols[b]);
        }

        var b1 = tuple.B1;
        while (b1 >= parameters.P)
        {
            b1 = (b1 + tuple.A1) % parameters.P1;
        }

        SymbolOperations.Xor(destination, intermediateSymbols[parameters.W + b1]);
        for (var j = 1; j < tuple.D1; j++)
        {
            b1 = (b1 + tuple.A1) % parameters.P1;
            while (b1 >= parameters.P)
            {
                b1 = (b1 + tuple.A1) % parameters.P1;
            }

            SymbolOperations.Xor(destination, intermediateSymbols[parameters.W + b1]);
        }
    }

    private static void Encode(
        FecParameters parameters,
        ReadOnlySpan<byte> intermediateSymbols,
        int symbolSize,
        FecTuple tuple,
        Span<byte> destination)
    {
        if (destination.Length == 0)
        {
            return;
        }

        var outputLength = destination.Length;
        var b = tuple.B;
        intermediateSymbols.Slice(b * symbolSize, outputLength).CopyTo(destination);
        for (var j = 1; j < tuple.D; j++)
        {
            b = (b + tuple.A) % parameters.W;
            SymbolOperations.Xor(destination, intermediateSymbols.Slice(b * symbolSize, outputLength));
        }

        var b1 = tuple.B1;
        while (b1 >= parameters.P)
        {
            b1 = (b1 + tuple.A1) % parameters.P1;
        }

        SymbolOperations.Xor(destination, intermediateSymbols.Slice((parameters.W + b1) * symbolSize, outputLength));
        for (var j = 1; j < tuple.D1; j++)
        {
            b1 = (b1 + tuple.A1) % parameters.P1;
            while (b1 >= parameters.P)
            {
                b1 = (b1 + tuple.A1) % parameters.P1;
            }

            SymbolOperations.Xor(destination, intermediateSymbols.Slice((parameters.W + b1) * symbolSize, outputLength));
        }
    }

    private static void ValidateSymbol(byte[] symbol, int symbolSize, string parameterName)
    {
        if (symbol.Length != symbolSize)
        {
            throw new ArgumentException("All symbols must have exactly T octets.", parameterName);
        }
    }

    private static EncodingPlan GetEncodingPlan(int sourceCount)
    {
        lock (PlanGate)
        {
            if (EncodingPlans.TryGetValue(sourceCount, out var plan))
            {
                return plan;
            }

            var parameters = FecParameters.ForSourceSymbolCount(sourceCount);
            var l = parameters.L;
            var matrix = new byte[checked(l * l)];
            FecMatrix.BuildPrecodeRows(parameters, matrix, l, l);

            var row = parameters.S + parameters.H;
            for (var x = 0; x < parameters.KPrime; x++, row++)
            {
                FecMatrix.BuildEncodingRow(parameters, (uint)x, matrix.AsSpan(row * l, l));
            }

            plan = new EncodingPlan(sourceCount, parameters, GaloisMatrix.Invert(matrix, l));
            EncodingPlans.Add(sourceCount, plan);
            return plan;
        }
    }

    private sealed class EncodingPlan(int sourceCount, FecParameters parameters, byte[] inverse)
    {
        private readonly Dictionary<int, byte[]> _sourceCoefficients = [];

        public int SourceCount { get; } = sourceCount;

        public FecParameters Parameters { get; } = parameters;

        public byte[] Inverse { get; } = inverse;

        public byte[] GetSourceCoefficients(int encodingSymbolId)
        {
            lock (_sourceCoefficients)
            {
                if (_sourceCoefficients.TryGetValue(encodingSymbolId, out var coefficients))
                {
                    return coefficients;
                }

                coefficients = BuildSourceCoefficients(encodingSymbolId);
                _sourceCoefficients.Add(encodingSymbolId, coefficients);
                return coefficients;
            }
        }

        private byte[] BuildSourceCoefficients(int encodingSymbolId)
        {
            var row = new byte[Parameters.L];
            var internalSymbolId = EncodingSymbolIdToInternalSymbolId(SourceCount, Parameters.KPrime, encodingSymbolId);
            FecMatrix.BuildEncodingRow(Parameters, internalSymbolId, row);

            var coefficients = new byte[SourceCount];
            var firstSourceRow = Parameters.S + Parameters.H;
            for (var intermediate = 0; intermediate < Parameters.L; intermediate++)
            {
                var factor = row[intermediate];
                if (factor == 0)
                {
                    continue;
                }

                var inverseRow = Inverse.AsSpan(intermediate * Parameters.L, Parameters.L);
                if (factor == 1)
                {
                    for (var source = 0; source < coefficients.Length; source++)
                    {
                        coefficients[source] ^= inverseRow[firstSourceRow + source];
                    }
                }
                else
                {
                    var multiplyRow = GaloisField256.GetMultiplyRow(factor);
                    for (var source = 0; source < coefficients.Length; source++)
                    {
                        coefficients[source] ^= multiplyRow[inverseRow[firstSourceRow + source]];
                    }
                }
            }

            return coefficients;
        }
    }
}
