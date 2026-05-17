using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace linker.fec.Internal;

internal static class SymbolOperations
{
    private static readonly Vector512<byte> LowMask512 = Vector512.Create((byte)0x0F);
    private static readonly Vector256<byte> LowMask256 = Vector256.Create((byte)0x0F);
    private static readonly Vector128<byte> LowMask128 = Vector128.Create((byte)0x0F);

    public static void Xor(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        if (destination.Length != source.Length)
        {
            throw new ArgumentException("Source and destination lengths must match.");
        }

        if (destination.Length == 0)
        {
            return;
        }

        var offset = 0;
        if (Avx512F.IsSupported)
        {
            ref var destinationRef = ref MemoryMarshal.GetReference(destination);
            ref var sourceRef = ref MemoryMarshal.GetReference(source);
            var vectorLength = destination.Length - (destination.Length % Vector512<byte>.Count);
            while (offset < vectorLength)
            {
                var left = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref destinationRef, offset));
                var right = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref sourceRef, offset));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Avx512F.Xor(left, right));
                offset += Vector512<byte>.Count;
            }
        }
        else if (Avx2.IsSupported)
        {
            ref var destinationRef = ref MemoryMarshal.GetReference(destination);
            ref var sourceRef = ref MemoryMarshal.GetReference(source);
            var vectorLength = destination.Length - (destination.Length % Vector256<byte>.Count);
            while (offset < vectorLength)
            {
                var left = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref destinationRef, offset));
                var right = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref sourceRef, offset));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Avx2.Xor(left, right));
                offset += Vector256<byte>.Count;
            }
        }
        else if (Sse2.IsSupported)
        {
            ref var destinationRef = ref MemoryMarshal.GetReference(destination);
            ref var sourceRef = ref MemoryMarshal.GetReference(source);
            var vectorLength = destination.Length - (destination.Length % Vector128<byte>.Count);
            while (offset < vectorLength)
            {
                var left = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref destinationRef, offset));
                var right = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref sourceRef, offset));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Sse2.Xor(left, right));
                offset += Vector128<byte>.Count;
            }
        }
        else if (AdvSimd.IsSupported)
        {
            ref var destinationRef = ref MemoryMarshal.GetReference(destination);
            ref var sourceRef = ref MemoryMarshal.GetReference(source);
            var vectorLength = destination.Length - (destination.Length % Vector128<byte>.Count);
            while (offset < vectorLength)
            {
                var left = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref destinationRef, offset));
                var right = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref sourceRef, offset));
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), AdvSimd.Xor(left, right));
                offset += Vector128<byte>.Count;
            }
        }

        var wideLength = destination.Length - ((destination.Length - offset) % sizeof(ulong));
        var destination64 = MemoryMarshal.Cast<byte, ulong>(destination[offset..wideLength]);
        var source64 = MemoryMarshal.Cast<byte, ulong>(source[offset..wideLength]);
        for (var i = 0; i < destination64.Length; i++)
        {
            destination64[i] ^= source64[i];
        }

        offset = wideLength;
        for (var i = wideLength; i < destination.Length; i++)
        {
            destination[i] ^= source[i];
        }
    }

    public static void AddScaled(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        if (destination.Length != source.Length)
        {
            throw new ArgumentException("Source and destination lengths must match.");
        }

        if (destination.Length == 0 || coefficient == 0)
        {
            return;
        }

        if (coefficient == 1)
        {
            Xor(destination, source);
            return;
        }

        var multiplyRow = GaloisField256.GetMultiplyRow(coefficient);
#if NET10_0_OR_GREATER
        if (Gfni.V512.IsSupported && destination.Length >= Vector512<byte>.Count)
        {
            AddScaledGfni512(destination, source, coefficient);
            return;
        }

        if (Gfni.V256.IsSupported && destination.Length >= Vector256<byte>.Count)
        {
            AddScaledGfni256(destination, source, coefficient);
            return;
        }
#endif

        if (Avx512BW.IsSupported && destination.Length >= Vector512<byte>.Count)
        {
            AddScaledAvx512(destination, source, multiplyRow, coefficient);
            return;
        }

        if (Avx2.IsSupported && destination.Length >= Vector256<byte>.Count)
        {
            AddScaledAvx2(destination, source, multiplyRow, coefficient);
            return;
        }

        if (Ssse3.IsSupported && destination.Length >= Vector128<byte>.Count)
        {
            AddScaledSsse3(destination, source, multiplyRow, coefficient);
            return;
        }

        if (AdvSimd.Arm64.IsSupported && destination.Length >= Vector128<byte>.Count)
        {
            AddScaledAdvSimdArm64(destination, source, multiplyRow, coefficient);
            return;
        }

        AddScaledScalar(destination, source, multiplyRow);
    }

    public static void ScaleTo(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        if (destination.Length != source.Length)
        {
            throw new ArgumentException("Source and destination lengths must match.");
        }

        if (destination.Length == 0)
        {
            return;
        }

        if (coefficient == 0)
        {
            destination.Clear();
            return;
        }

        if (coefficient == 1)
        {
            source.CopyTo(destination);
            return;
        }

        var multiplyRow = GaloisField256.GetMultiplyRow(coefficient);
#if NET10_0_OR_GREATER
        if (Gfni.V512.IsSupported && destination.Length >= Vector512<byte>.Count)
        {
            ScaleToGfni512(destination, source, coefficient);
            return;
        }

        if (Gfni.V256.IsSupported && destination.Length >= Vector256<byte>.Count)
        {
            ScaleToGfni256(destination, source, coefficient);
            return;
        }
#endif

        if (Avx512BW.IsSupported && destination.Length >= Vector512<byte>.Count)
        {
            ScaleToAvx512(destination, source, multiplyRow, coefficient);
            return;
        }

        if (Avx2.IsSupported && destination.Length >= Vector256<byte>.Count)
        {
            ScaleToAvx2(destination, source, multiplyRow, coefficient);
            return;
        }

        if (Ssse3.IsSupported && destination.Length >= Vector128<byte>.Count)
        {
            ScaleToSsse3(destination, source, multiplyRow, coefficient);
            return;
        }

        if (AdvSimd.Arm64.IsSupported && destination.Length >= Vector128<byte>.Count)
        {
            ScaleToAdvSimdArm64(destination, source, multiplyRow, coefficient);
            return;
        }

        ScaleToScalar(destination, source, multiplyRow);
    }

    public static void ScaleToPadded(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        if (source.Length > destination.Length)
        {
            throw new ArgumentException("Source cannot be longer than destination.", nameof(source));
        }

        if (source.Length == 0)
        {
            destination.Clear();
            return;
        }

        ScaleTo(destination[..source.Length], source, coefficient);
        destination[source.Length..].Clear();
    }

    public static void AddScaledPadded(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        if (source.Length > destination.Length)
        {
            throw new ArgumentException("Source cannot be longer than destination.", nameof(source));
        }

        if (source.Length == 0)
        {
            return;
        }

        AddScaled(destination[..source.Length], source, coefficient);
    }

    public static void MultiplyInPlace(Span<byte> value, byte coefficient)
    {
        if (coefficient == 0)
        {
            value.Clear();
            return;
        }

        if (coefficient == 1)
        {
            return;
        }

        var multiplyRow = GaloisField256.GetMultiplyRow(coefficient);
        if (Avx2.IsSupported && value.Length >= Vector256<byte>.Count)
        {
            ScaleToAvx2(value, value, multiplyRow, coefficient);
            return;
        }

        if (Ssse3.IsSupported && value.Length >= Vector128<byte>.Count)
        {
            ScaleToSsse3(value, value, multiplyRow, coefficient);
            return;
        }

        if (AdvSimd.Arm64.IsSupported && value.Length >= Vector128<byte>.Count)
        {
            ScaleToAdvSimdArm64(value, value, multiplyRow, coefficient);
            return;
        }

        ScaleToScalar(value, value, multiplyRow);
    }

    private static void AddScaledAvx2(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var lowLookup = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var highLookup = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector256<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVector(input, lowLookup, highLookup, LowMask256);
            var output = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref destinationRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Avx2.Xor(output, scaled));
            offset += Vector256<byte>.Count;
        }

        AddScaledScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static void AddScaledAvx512(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var low256 = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var high256 = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        var lowLookup = Vector512.Create(low256, low256);
        var highLookup = Vector512.Create(high256, high256);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector512<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVector(input, lowLookup, highLookup, LowMask512);
            var output = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref destinationRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Avx512F.Xor(output, scaled));
            offset += Vector512<byte>.Count;
        }

        AddScaledScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static void ScaleToAvx2(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var lowLookup = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var highLookup = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector256<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVector(input, lowLookup, highLookup, LowMask256);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), scaled);
            offset += Vector256<byte>.Count;
        }

        ScaleToScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static void ScaleToAvx512(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var low256 = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var high256 = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        var lowLookup = Vector512.Create(low256, low256);
        var highLookup = Vector512.Create(high256, high256);
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector512<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVector(input, lowLookup, highLookup, LowMask512);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), scaled);
            offset += Vector512<byte>.Count;
        }

        ScaleToScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static void AddScaledAdvSimdArm64(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var lowLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var highLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector128<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVectorAdvSimdArm64(input, lowLookup, highLookup);
            var output = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref destinationRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), AdvSimd.Xor(output, scaled));
            offset += Vector128<byte>.Count;
        }

        AddScaledScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static void ScaleToAdvSimdArm64(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var lowLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var highLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector128<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), MultiplyVectorAdvSimdArm64(input, lowLookup, highLookup));
            offset += Vector128<byte>.Count;
        }

        ScaleToScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static Vector256<byte> MultiplyVector(
        Vector256<byte> input,
        Vector256<byte> lowLookup,
        Vector256<byte> highLookup,
        Vector256<byte> lowMask)
    {
        var low = Avx2.And(input, lowMask);
        var high = Avx2.And(Avx2.ShiftRightLogical(input.AsUInt16(), 4).AsByte(), lowMask);
        return Avx2.Xor(Avx2.Shuffle(lowLookup, low), Avx2.Shuffle(highLookup, high));
    }

    private static Vector512<byte> MultiplyVector(
        Vector512<byte> input,
        Vector512<byte> lowLookup,
        Vector512<byte> highLookup,
        Vector512<byte> lowMask)
    {
        var low = Avx512F.And(input, lowMask);
        var high = Avx512F.And(Avx512BW.ShiftRightLogical(input.AsUInt16(), 4).AsByte(), lowMask);
        return Avx512F.Xor(Avx512BW.Shuffle(lowLookup, low), Avx512BW.Shuffle(highLookup, high));
    }

    private static Vector128<byte> MultiplyVectorAdvSimdArm64(
        Vector128<byte> input,
        Vector128<byte> lowLookup,
        Vector128<byte> highLookup)
    {
        var low = AdvSimd.And(input, LowMask128);
        var high = AdvSimd.And(AdvSimd.ShiftRightLogical(input, 4), LowMask128);
        return AdvSimd.Xor(
            AdvSimd.Arm64.VectorTableLookup(lowLookup, low),
            AdvSimd.Arm64.VectorTableLookup(highLookup, high));
    }

#if NET10_0_OR_GREATER
    private static void AddScaledGfni512(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        var matrix = Vector512.Create(GaloisField256.GetAffineMultiplyMatrix(coefficient)).AsByte();
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector512<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = Gfni.V512.GaloisFieldAffineTransform(input, matrix, 0);
            var output = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref destinationRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Avx512F.Xor(output, scaled));
            offset += Vector512<byte>.Count;
        }

        AddScaledScalar(destination[offset..], source[offset..], GaloisField256.GetMultiplyRow(coefficient));
    }

    private static void ScaleToGfni512(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        var matrix = Vector512.Create(GaloisField256.GetAffineMultiplyMatrix(coefficient)).AsByte();
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector512<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector512<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = Gfni.V512.GaloisFieldAffineTransform(input, matrix, 0);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), scaled);
            offset += Vector512<byte>.Count;
        }

        ScaleToScalar(destination[offset..], source[offset..], GaloisField256.GetMultiplyRow(coefficient));
    }

    private static void AddScaledGfni256(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        var matrix = Vector256.Create(GaloisField256.GetAffineMultiplyMatrix(coefficient)).AsByte();
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector256<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = Gfni.V256.GaloisFieldAffineTransform(input, matrix, 0);
            var output = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref destinationRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Avx2.Xor(output, scaled));
            offset += Vector256<byte>.Count;
        }

        AddScaledScalar(destination[offset..], source[offset..], GaloisField256.GetMultiplyRow(coefficient));
    }

    private static void ScaleToGfni256(Span<byte> destination, ReadOnlySpan<byte> source, byte coefficient)
    {
        var matrix = Vector256.Create(GaloisField256.GetAffineMultiplyMatrix(coefficient)).AsByte();
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector256<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = Gfni.V256.GaloisFieldAffineTransform(input, matrix, 0);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), scaled);
            offset += Vector256<byte>.Count;
        }

        ScaleToScalar(destination[offset..], source[offset..], GaloisField256.GetMultiplyRow(coefficient));
    }
#endif

    private static void AddScaledSsse3(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var lowLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var highLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector128<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVector(input, lowLookup, highLookup, LowMask128);
            var output = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref destinationRef, offset));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), Sse2.Xor(output, scaled));
            offset += Vector128<byte>.Count;
        }

        AddScaledScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static void ScaleToSsse3(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow, byte coefficient)
    {
        var lowLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetLowNibbleLookup(coefficient)));
        var highLookup = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(GaloisField256.GetHighNibbleLookup(coefficient)));
        ref var destinationRef = ref MemoryMarshal.GetReference(destination);
        ref var sourceRef = ref MemoryMarshal.GetReference(source);

        var offset = 0;
        var vectorLength = destination.Length - (destination.Length % Vector128<byte>.Count);
        while (offset < vectorLength)
        {
            var input = Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.Add(ref sourceRef, offset));
            var scaled = MultiplyVector(input, lowLookup, highLookup, LowMask128);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destinationRef, offset), scaled);
            offset += Vector128<byte>.Count;
        }

        ScaleToScalar(destination[offset..], source[offset..], multiplyRow);
    }

    private static Vector128<byte> MultiplyVector(
        Vector128<byte> input,
        Vector128<byte> lowLookup,
        Vector128<byte> highLookup,
        Vector128<byte> lowMask)
    {
        var low = Sse2.And(input, lowMask);
        var high = Sse2.And(Sse2.ShiftRightLogical(input.AsUInt16(), 4).AsByte(), lowMask);
        return Sse2.Xor(Ssse3.Shuffle(lowLookup, low), Ssse3.Shuffle(highLookup, high));
    }

    private static void AddScaledScalar(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] ^= multiplyRow[source[i]];
        }
    }

    private static void ScaleToScalar(Span<byte> destination, ReadOnlySpan<byte> source, ReadOnlySpan<byte> multiplyRow)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] = multiplyRow[source[i]];
        }
    }
}
