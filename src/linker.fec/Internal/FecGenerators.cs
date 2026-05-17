using System;

namespace linker.fec.Internal;

internal static class FecGenerators
{
    private static ReadOnlySpan<int> DegreeDistribution =>
    [
        0, 5243, 529531, 704294, 791675, 844104, 879057, 904023,
        922747, 937311, 948962, 958494, 966438, 973160, 978921,
        983914, 988283, 992138, 995565, 998631, 1001391, 1003887,
        1006157, 1008229, 1010129, 1011876, 1013490, 1014983,
        1016370, 1017662, 1048576
    ];

    public static uint Rand(uint y, int i, uint m)
    {
        if ((uint)i >= 256)
        {
            throw new ArgumentOutOfRangeException(nameof(i), i, "The FEC Rand index must be in [0, 255].");
        }

        if (m == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(m), m, "The modulo value must be positive.");
        }

        var x0 = (byte)(y + (uint)i);
        var x1 = (byte)((y >> 8) + (uint)i);
        var x2 = (byte)((y >> 16) + (uint)i);
        var x3 = (byte)((y >> 24) + (uint)i);
        return (FecTables.V0[x0] ^ FecTables.V1[x1] ^ FecTables.V2[x2] ^ FecTables.V3[x3]) % m;
    }

    public static int Deg(uint v, int w)
    {
        if (v >= 1_048_576)
        {
            throw new ArgumentOutOfRangeException(nameof(v), v, "The degree input must be less than 2^20.");
        }

        for (var d = 1; d < DegreeDistribution.Length; d++)
        {
            if (v < DegreeDistribution[d])
            {
                return Math.Min(d, w - 2);
            }
        }

        throw new InvalidOperationException("Invalid FEC degree distribution.");
    }

    public static FecTuple Tuple(FecParameters parameters, uint internalSymbolId)
    {
        var a = 53_591u + ((uint)parameters.J * 997u);
        if ((a & 1) == 0)
        {
            a++;
        }

        var b = 10_267u * ((uint)parameters.J + 1u);
        var y = b + (internalSymbolId * a);
        var v = Rand(y, 0, 1u << 20);
        var d = Deg(v, parameters.W);
        var ltA = 1 + (int)Rand(y, 1, (uint)(parameters.W - 1));
        var ltB = (int)Rand(y, 2, (uint)parameters.W);
        var d1 = d < 4 ? 2 + (int)Rand(internalSymbolId, 3, 2) : 2;
        var piA = 1 + (int)Rand(internalSymbolId, 4, (uint)(parameters.P1 - 1));
        var piB = (int)Rand(internalSymbolId, 5, (uint)parameters.P1);

        return new FecTuple(d, ltA, ltB, d1, piA, piB);
    }
}
