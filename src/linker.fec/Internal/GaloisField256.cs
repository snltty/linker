using System;

namespace linker.fec.Internal;

internal static class GaloisField256
{
    private const int PrimitivePolynomial = 0x11D;
    private static readonly byte[] Exp = new byte[512];
    private static readonly byte[] Log = new byte[256];
    private static readonly byte[] MultiplyTable = new byte[256 * 256];
    private static readonly byte[] LowNibbleLookup = new byte[256 * 32];
    private static readonly byte[] HighNibbleLookup = new byte[256 * 32];
    private static readonly byte[] InverseTable = new byte[256];
#if NET10_0_OR_GREATER
    private static readonly ulong[] AffineMultiplyTable = BuildAffineMultiplyTable();
#endif

    static GaloisField256()
    {
        var x = 1;
        for (var i = 0; i < 255; i++)
        {
            Exp[i] = (byte)x;
            Log[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0)
            {
                x ^= PrimitivePolynomial;
            }
        }

        for (var i = 255; i < Exp.Length; i++)
        {
            Exp[i] = Exp[i - 255];
        }

        for (var a = 0; a < 256; a++)
        {
            for (var b = 0; b < 256; b++)
            {
                MultiplyTable[(a << 8) | b] = MultiplySlow((byte)a, (byte)b);
            }
        }

        for (var a = 0; a < 256; a++)
        {
            var multiplyRow = MultiplyTable.AsSpan(a << 8, 256);
            var lowLookup = LowNibbleLookup.AsSpan(a * 32, 32);
            var highLookup = HighNibbleLookup.AsSpan(a * 32, 32);
            for (var i = 0; i < 16; i++)
            {
                var low = multiplyRow[i];
                var high = multiplyRow[i << 4];
                for (var j = i; j < lowLookup.Length; j += 16)
                {
                    lowLookup[j] = low;
                    highLookup[j] = high;
                }
            }
        }

        for (var i = 1; i < 256; i++)
        {
            InverseTable[i] = Exp[255 - Log[i]];
        }
    }

    public static byte Multiply(byte left, byte right)
    {
        return MultiplyTable[(left << 8) | right];
    }

    public static ReadOnlySpan<byte> GetMultiplyRow(byte left)
    {
        return MultiplyTable.AsSpan(left << 8, 256);
    }

    public static ReadOnlySpan<byte> GetLowNibbleLookup(byte left)
    {
        return LowNibbleLookup.AsSpan(left * 32, 32);
    }

    public static ReadOnlySpan<byte> GetHighNibbleLookup(byte left)
    {
        return HighNibbleLookup.AsSpan(left * 32, 32);
    }

#if NET10_0_OR_GREATER
    public static ulong GetAffineMultiplyMatrix(byte left)
    {
        return AffineMultiplyTable[left];
    }
#endif

    public static byte AlphaPower(int exponent)
    {
        if (exponent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "Exponent cannot be negative.");
        }

        return Exp[exponent % 255];
    }

    public static byte Divide(byte left, byte right)
    {
        if (right == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero in GF(256).");
        }

        return left == 0 ? (byte)0 : Exp[Log[left] + 255 - Log[right]];
    }

    public static byte Inverse(byte value)
    {
        if (value == 0)
        {
            throw new DivideByZeroException("Zero has no inverse in GF(256).");
        }

        return InverseTable[value];
    }

    private static byte MultiplySlow(byte left, byte right)
    {
        if (left == 0 || right == 0)
        {
            return 0;
        }

        return Exp[Log[left] + Log[right]];
    }

#if NET10_0_OR_GREATER
    private static ulong[] BuildAffineMultiplyTable()
    {
        var table = new ulong[256];
        for (var scalar = 0; scalar < table.Length; scalar++)
        {
            table[scalar] = BuildAffineMultiplyMatrix((byte)scalar);
        }

        return table;
    }

    private static ulong BuildAffineMultiplyMatrix(byte scalar)
    {
        var matrix = new byte[8];
        for (var bit = 0; bit < 8; bit++)
        {
            var basis = (byte)(1 << bit);
            var product = Multiply(scalar, basis);
            for (var row = 0; row < 8; row++)
            {
                if ((product & (1 << row)) != 0)
                {
                    matrix[row] |= (byte)(1 << bit);
                }
            }
        }

        var packed = 0UL;
        for (var row = 0; row < matrix.Length; row++)
        {
            packed |= (ulong)matrix[row] << ((7 - row) * 8);
        }

        return packed;
    }
#endif
}
