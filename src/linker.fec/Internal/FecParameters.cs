using System;

namespace linker.fec.Internal;

internal readonly record struct FecParameters(int KPrime, int J, int S, int H, int W)
{
    public int L => KPrime + S + H;

    public int P => L - W;

    public int P1 => PrimeAtLeast(P);

    public int U => P - H;

    public int B => W - S;

    public static FecParameters ForSourceSymbolCount(int sourceSymbolCount)
    {
        if (sourceSymbolCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSymbolCount), sourceSymbolCount, "Source symbol count must be positive.");
        }

        foreach (var parameters in FecTables.Parameters)
        {
            if (parameters.KPrime >= sourceSymbolCount)
            {
                return parameters;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(sourceSymbolCount), sourceSymbolCount,
            $"This compact frame format supports at most {LinkerFecOptions.MaxSourceSymbolsPerBlock} source symbols per block.");
    }

    private static int PrimeAtLeast(int value)
    {
        var candidate = Math.Max(2, value);
        while (!IsPrime(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    private static bool IsPrime(int value)
    {
        if (value < 2)
        {
            return false;
        }

        if (value == 2)
        {
            return true;
        }

        if ((value & 1) == 0)
        {
            return false;
        }

        for (var divisor = 3; divisor * divisor <= value; divisor += 2)
        {
            if (value % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }
}
