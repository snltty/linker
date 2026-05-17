using System;

namespace linker.fec.Internal;

internal static class FecMatrix
{
    public static void BuildPrecodeRows(FecParameters parameters, byte[] matrix, int rowCount, int columnCount)
    {
        if (columnCount != parameters.L || rowCount < parameters.S + parameters.H)
        {
            throw new ArgumentException("Invalid FEC precode matrix dimensions.", nameof(matrix));
        }

        AddLdpcRows(parameters, matrix, columnCount);
        AddHdpcRows(parameters, matrix, columnCount);
    }

    public static void BuildEncodingRow(FecParameters parameters, uint internalSymbolId, Span<byte> row)
    {
        if (row.Length != parameters.L)
        {
            throw new ArgumentException("Encoding row length must equal L.", nameof(row));
        }

        row.Clear();
        AddEncodingCoefficients(parameters, FecGenerators.Tuple(parameters, internalSymbolId), row);
    }

    public static void AddEncodingCoefficients(FecParameters parameters, FecTuple tuple, Span<byte> row)
    {
        var b = tuple.B;
        row[b] ^= 1;
        for (var j = 1; j < tuple.D; j++)
        {
            b = (b + tuple.A) % parameters.W;
            row[b] ^= 1;
        }

        var b1 = tuple.B1;
        while (b1 >= parameters.P)
        {
            b1 = (b1 + tuple.A1) % parameters.P1;
        }

        row[parameters.W + b1] ^= 1;
        for (var j = 1; j < tuple.D1; j++)
        {
            b1 = (b1 + tuple.A1) % parameters.P1;
            while (b1 >= parameters.P)
            {
                b1 = (b1 + tuple.A1) % parameters.P1;
            }

            row[parameters.W + b1] ^= 1;
        }
    }

    private static void AddLdpcRows(FecParameters p, byte[] matrix, int stride)
    {
        for (var i = 0; i < p.S; i++)
        {
            matrix[(i * stride) + p.B + i] ^= 1;
        }

        for (var i = 0; i < p.B; i++)
        {
            var a = 1 + (i / p.S);
            var b = i % p.S;

            matrix[(b * stride) + i] ^= 1;
            b = (b + a) % p.S;
            matrix[(b * stride) + i] ^= 1;
            b = (b + a) % p.S;
            matrix[(b * stride) + i] ^= 1;
        }

        for (var i = 0; i < p.S; i++)
        {
            var a = i % p.P;
            var b = (i + 1) % p.P;
            matrix[(i * stride) + p.W + a] ^= 1;
            matrix[(i * stride) + p.W + b] ^= 1;
        }
    }

    private static void AddHdpcRows(FecParameters p, byte[] matrix, int stride)
    {
        var kps = p.KPrime + p.S;

        for (var j = 0; j < kps - 1; j++)
        {
            var first = (int)FecGenerators.Rand((uint)j + 1, 6, (uint)p.H);
            var second = (first + (int)FecGenerators.Rand((uint)j + 1, 7, (uint)(p.H - 1)) + 1) % p.H;
            AddGammaColumnContribution(matrix, stride, p.S + first, j, 1);
            AddGammaColumnContribution(matrix, stride, p.S + second, j, 1);
        }

        var lastColumn = kps - 1;
        for (var i = 0; i < p.H; i++)
        {
            AddGammaColumnContribution(matrix, stride, p.S + i, lastColumn, GaloisField256.AlphaPower(i));
            matrix[((p.S + i) * stride) + kps + i] ^= 1;
        }
    }

    private static void AddGammaColumnContribution(byte[] matrix, int stride, int row, int gammaRow, byte mtValue)
    {
        var rowOffset = row * stride;
        if (mtValue == 1)
        {
            for (var column = 0; column <= gammaRow; column++)
            {
                matrix[rowOffset + column] ^= GaloisField256.AlphaPower(gammaRow - column);
            }

            return;
        }

        var multiplyRow = GaloisField256.GetMultiplyRow(mtValue);
        for (var column = 0; column <= gammaRow; column++)
        {
            matrix[rowOffset + column] ^= multiplyRow[GaloisField256.AlphaPower(gammaRow - column)];
        }
    }
}
