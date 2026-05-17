using System;

namespace linker.fec.Internal;

internal static class LinearSystemSolver
{
    public static bool TrySolve(
        byte[] matrix,
        int rowCount,
        int unknownCount,
        byte[] values,
        int symbolSize,
        out byte[][] symbols)
    {
        symbols = [];
        if (rowCount < unknownCount)
        {
            return false;
        }

        if (matrix.Length != rowCount * unknownCount)
        {
            throw new ArgumentException("Invalid matrix dimensions.", nameof(matrix));
        }

        if (values.Length != rowCount * symbolSize)
        {
            throw new ArgumentException("Invalid value dimensions.", nameof(values));
        }

        var pivotColumns = new int[unknownCount];
        if (!TrySolveInPlace(matrix, rowCount, unknownCount, values, symbolSize, pivotColumns))
        {
            return false;
        }

        symbols = new byte[unknownCount][];
        for (var row = 0; row < unknownCount; row++)
        {
            symbols[pivotColumns[row]] = values.AsSpan(row * symbolSize, symbolSize).ToArray();
        }

        return true;
    }

    public static bool TrySolveInPlace(
        Span<byte> matrix,
        int rowCount,
        int unknownCount,
        Span<byte> values,
        int symbolSize,
        Span<int> pivotColumns)
    {
        if (rowCount < unknownCount)
        {
            return false;
        }

        if (matrix.Length != rowCount * unknownCount)
        {
            throw new ArgumentException("Invalid matrix dimensions.", nameof(matrix));
        }

        if (values.Length != rowCount * symbolSize)
        {
            throw new ArgumentException("Invalid value dimensions.", nameof(values));
        }

        if (pivotColumns.Length < unknownCount)
        {
            throw new ArgumentException("Pivot column buffer is too small.", nameof(pivotColumns));
        }

        var rank = 0;
        for (var column = 0; column < unknownCount && rank < rowCount; column++)
        {
            var pivot = FindPivot(matrix, rowCount, unknownCount, rank, column);
            if (pivot < 0)
            {
                continue;
            }

            if (pivot != rank)
            {
                SwapRows(matrix, values, unknownCount, symbolSize, pivot, rank);
            }

            NormalizeRow(matrix, values, unknownCount, symbolSize, rank, column);
            EliminateColumn(matrix, values, rowCount, unknownCount, symbolSize, rank, column);
            pivotColumns[rank] = column;
            rank++;
        }

        return rank >= unknownCount;
    }

    private static int FindPivot(ReadOnlySpan<byte> matrix, int rowCount, int columnCount, int startRow, int column)
    {
        for (var row = startRow; row < rowCount; row++)
        {
            if (matrix[(row * columnCount) + column] != 0)
            {
                return row;
            }
        }

        return -1;
    }

    private static void SwapRows(Span<byte> matrix, Span<byte> values, int columnCount, int symbolSize, int left, int right)
    {
        Swap(matrix.Slice(left * columnCount, columnCount), matrix.Slice(right * columnCount, columnCount));
        Swap(values.Slice(left * symbolSize, symbolSize), values.Slice(right * symbolSize, symbolSize));
    }

    private static void NormalizeRow(Span<byte> matrix, Span<byte> values, int columnCount, int symbolSize, int row, int pivotColumn)
    {
        var rowStart = row * columnCount;
        var pivot = matrix[rowStart + pivotColumn];
        if (pivot == 1)
        {
            return;
        }

        var inverse = GaloisField256.Inverse(pivot);
        var matrixMultiplyRow = GaloisField256.GetMultiplyRow(inverse);
        for (var column = pivotColumn; column < columnCount; column++)
        {
            matrix[rowStart + column] = matrixMultiplyRow[matrix[rowStart + column]];
        }

        var value = values.Slice(row * symbolSize, symbolSize);
        var valueMultiplyRow = matrixMultiplyRow;
        for (var i = 0; i < value.Length; i++)
        {
            value[i] = valueMultiplyRow[value[i]];
        }
    }

    private static void EliminateColumn(
        Span<byte> matrix,
        Span<byte> values,
        int rowCount,
        int columnCount,
        int symbolSize,
        int pivotRow,
        int pivotColumn)
    {
        var pivotMatrix = matrix.Slice(pivotRow * columnCount, columnCount);
        var pivotValue = values.Slice(pivotRow * symbolSize, symbolSize);

        for (var row = 0; row < rowCount; row++)
        {
            if (row == pivotRow)
            {
                continue;
            }

            var rowStart = row * columnCount;
            var factor = matrix[rowStart + pivotColumn];
            if (factor == 0)
            {
                continue;
            }

            var targetMatrix = matrix.Slice(rowStart, columnCount);
            AddScaledSuffix(targetMatrix[pivotColumn..], pivotMatrix[pivotColumn..], factor);
            SymbolOperations.AddScaled(values.Slice(row * symbolSize, symbolSize), pivotValue, factor);
        }
    }

    private static void AddScaledSuffix(Span<byte> target, ReadOnlySpan<byte> pivot, byte factor)
    {
        if (factor == 1)
        {
            for (var i = 0; i < target.Length; i++)
            {
                target[i] ^= pivot[i];
            }

            return;
        }

        var multiplyRow = GaloisField256.GetMultiplyRow(factor);
        for (var i = 0; i < target.Length; i++)
        {
            target[i] ^= multiplyRow[pivot[i]];
        }
    }

    private static void Swap(Span<byte> left, Span<byte> right)
    {
        for (var i = 0; i < left.Length; i++)
        {
            (left[i], right[i]) = (right[i], left[i]);
        }
    }
}
