using System;
using System.Linq;

namespace linker.fec.Internal;

internal static class GaloisMatrix
{
    public static byte[] Invert(byte[] matrix, int size)
    {
        if (matrix.Length != size * size)
        {
            throw new ArgumentException("Matrix must be square.", nameof(matrix));
        }

        var working = matrix.ToArray();
        var inverse = new byte[checked(size * size)];
        for (var i = 0; i < size; i++)
        {
            inverse[(i * size) + i] = 1;
        }

        for (var column = 0; column < size; column++)
        {
            var pivot = FindPivot(working, size, column, column);
            if (pivot < 0)
            {
                throw new InvalidOperationException("Matrix is singular.");
            }

            if (pivot != column)
            {
                SwapRows(working, size, pivot, column);
                SwapRows(inverse, size, pivot, column);
            }

            NormalizeRows(working, inverse, size, column, column);
            EliminateColumn(working, inverse, size, column, column);
        }

        return inverse;
    }

    public static void MultiplyBySourceRows(
        byte[] inverse,
        int size,
        int firstSourceRow,
        byte[][] sourceSymbols,
        byte[][] destination)
    {
        if (destination.Length != size || inverse.Length != size * size)
        {
            throw new ArgumentException("Invalid inverse matrix dimensions.", nameof(inverse));
        }

        for (var output = 0; output < size; output++)
        {
            var outputSymbol = destination[output];
            var inverseRow = inverse.AsSpan(output * size, size);
            for (var source = 0; source < sourceSymbols.Length; source++)
            {
                SymbolOperations.AddScaled(outputSymbol, sourceSymbols[source], inverseRow[firstSourceRow + source]);
            }
        }
    }

    private static int FindPivot(byte[] matrix, int size, int startRow, int column)
    {
        for (var row = startRow; row < size; row++)
        {
            if (matrix[(row * size) + column] != 0)
            {
                return row;
            }
        }

        return -1;
    }

    private static void NormalizeRows(byte[] matrix, byte[] inverse, int size, int row, int pivotColumn)
    {
        var pivot = matrix[(row * size) + pivotColumn];
        if (pivot == 1)
        {
            return;
        }

        var scale = GaloisField256.Inverse(pivot);
        ScaleRow(matrix.AsSpan(row * size, size), scale, pivotColumn);
        ScaleRow(inverse.AsSpan(row * size, size), scale, 0);
    }

    private static void EliminateColumn(byte[] matrix, byte[] inverse, int size, int pivotRow, int pivotColumn)
    {
        var pivotMatrix = matrix.AsSpan(pivotRow * size, size);
        var pivotInverse = inverse.AsSpan(pivotRow * size, size);
        for (var row = 0; row < size; row++)
        {
            if (row == pivotRow)
            {
                continue;
            }

            var rowOffset = row * size;
            var factor = matrix[rowOffset + pivotColumn];
            if (factor == 0)
            {
                continue;
            }

            AddScaled(matrix.AsSpan(rowOffset, size)[pivotColumn..], pivotMatrix[pivotColumn..], factor);
            AddScaled(inverse.AsSpan(rowOffset, size), pivotInverse, factor);
        }
    }

    private static void ScaleRow(Span<byte> row, byte factor, int start)
    {
        var multiplyRow = GaloisField256.GetMultiplyRow(factor);
        for (var i = start; i < row.Length; i++)
        {
            row[i] = multiplyRow[row[i]];
        }
    }

    private static void AddScaled(Span<byte> destination, ReadOnlySpan<byte> source, byte factor)
    {
        if (factor == 1)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] ^= source[i];
            }

            return;
        }

        var multiplyRow = GaloisField256.GetMultiplyRow(factor);
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] ^= multiplyRow[source[i]];
        }
    }

    private static void SwapRows(byte[] matrix, int size, int left, int right)
    {
        var leftRow = matrix.AsSpan(left * size, size);
        var rightRow = matrix.AsSpan(right * size, size);
        for (var i = 0; i < leftRow.Length; i++)
        {
            (leftRow[i], rightRow[i]) = (rightRow[i], leftRow[i]);
        }
    }
}
