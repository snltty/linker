using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace common.libs.extends
{
    public static class NumberExtends
    {
        #region 序列化
        #region double
        public static byte[] ToBytes(this double num)
        {
            return BitConverter.GetBytes(num);
        }
        public static unsafe void ToBytes(this double value, Memory<byte> memory)
        {
            ref double v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 8).CopyTo(memory.Span);
            }
        }
        #endregion

        #region 64
        public static byte[] ToBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }
        public static unsafe void ToBytes(this long value, Memory<byte> memory)
        {
            ref long v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 8).CopyTo(memory.Span);
            }
        }
        public static unsafe void ToBytes(this long[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(long)).CopyTo(memory.Span);
            }
        }
        public static byte[] ToBytes(this ulong value)
        {
            return BitConverter.GetBytes(value);
        }
        public static unsafe void ToBytes(this ulong value, Memory<byte> memory)
        {
            ref ulong v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, sizeof(ulong)).CopyTo(memory.Span);
            }
        }
        public static unsafe void ToBytes(this ulong[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * 8).CopyTo(memory.Span);
            }
        }
        public static unsafe void ToBytes(this Memory<ulong> value, Memory<byte> memory)
        {
            fixed (void* p = &value.Span[0])
            {
                new Span<byte>(p, value.Length * 8).CopyTo(memory.Span);
            }
        }
        #endregion

        #region 32
        public static byte[] ToBytes(this int num)
        {
            return BitConverter.GetBytes(num);
        }
        public static unsafe void ToBytes(this int value, Memory<byte> memory)
        {
            ref int v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 4).CopyTo(memory.Span);
            }
        }
        public static unsafe void ToBytes(this int[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(int)).CopyTo(memory.Span);
            }
        }
        public static byte[] ToBytes(this uint num)
        {
            return BitConverter.GetBytes(num);
        }
        public static unsafe void ToBytes(this uint value, Memory<byte> memory)
        {
            ref uint v = ref value;
            fixed (void* p = &v)
            {
                new Span<byte>(p, 4).CopyTo(memory.Span);
            }
        }
        public static unsafe void ToBytes(this uint[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(uint)).CopyTo(memory.Span);
            }
        }
        #endregion

        #region 16
        public static byte[] ToBytes(this short num)
        {
            return BitConverter.GetBytes(num);
        }
        public static unsafe void ToBytes(this short value, Memory<byte> memory)
        {
            ref short v = ref value;
            fixed (void* p = &v)
            {
                var span = new Span<byte>(p, sizeof(short));
                memory.Span[0] = span[0];
                memory.Span[1] = span[1];
            }
        }
        public static unsafe void ToBytes(this short[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(short)).CopyTo(memory.Span);
            }
        }

        public static byte[] ToBytes(this ushort num)
        {
            return BitConverter.GetBytes(num);
        }
        public static unsafe void ToBytes(this ushort value, Memory<byte> memory)
        {
            ref ushort v = ref value;
            fixed (void* p = &v)
            {
                var span = new Span<byte>(p, sizeof(ushort));
                memory.Span[0] = span[0];
                memory.Span[1] = span[1];
            }
        }
        public static unsafe void ToBytes(this ushort[] value, Memory<byte> memory)
        {
            fixed (void* p = &value[0])
            {
                new Span<byte>(p, value.Length * sizeof(ushort)).CopyTo(memory.Span);
            }
        }
        #endregion
        #endregion

        #region 反序列化

        #region double
        public static double ToDouble(this byte[] bytes, int startindex = 0)
        {
            return Unsafe.As<byte, double>(ref bytes[startindex]);
        }
        public static double ToDouble(this Span<byte> span)
        {
            return Unsafe.As<byte, double>(ref MemoryMarshal.GetReference(span));
        }
        public static double ToDouble(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, double>(ref MemoryMarshal.GetReference(span));
        }
        #endregion

        #region 64
        public static long ToInt64(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(span));
        }
        public static long ToInt64(this Span<byte> span)
        {
            return Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(span));
        }
        public static long ToInt64(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToInt64();
        }
        public static long ToInt64(this Memory<byte> memory)
        {
            return memory.Span.ToInt64();
        }
        public static ulong ToUInt64(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(span));
        }
        public static ulong ToUInt64(this Span<byte> span)
        {
            return Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(span));
        }
        public static ulong ToUInt64(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToUInt64();
        }
        public static ulong ToUInt64(this Memory<byte> memory)
        {
            return memory.Span.ToUInt64();
        }
        #endregion

        #region 32
        public static int ToInt32(this byte[] bytes, int startindex = 0)
        {
            return Unsafe.As<byte, ushort>(ref bytes[startindex]);
        }
        public static int ToInt32(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(span));
        }
        public static int ToInt32(this Span<byte> span)
        {
            return Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(span));
        }
        public static int ToInt32(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToInt32();
        }
        public static int ToInt32(this Memory<byte> memory)
        {
            return memory.Span.ToInt32();
        }
        public static uint ToUInt32(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(span));
        }
        public static uint ToUInt32(this Span<byte> span)
        {
            return Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(span));
        }
        public static uint ToUInt32(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToUInt32();
        }
        public static uint ToUInt32(this Memory<byte> memory)
        {
            return memory.Span.ToUInt32();
        }
        #endregion

        #region 16
        public static short ToInt16(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, short>(ref MemoryMarshal.GetReference(span));
        }
        public static short ToInt16(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToInt16();
        }
        public static short[] ToInt16Array(this ReadOnlySpan<byte> span)
        {
            short[] res = new short[span.Length / 2];
            int index = 0;
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = span.Slice(index, 2).ToInt16();
                index += 2;
            }
            return res;
        }
        public static short[] ToInt16Array(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToInt16Array();
        }

        public static ushort ToUInt16(this ReadOnlySpan<byte> span)
        {
            return Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(span));
        }
        public static ushort ToUInt16(this Span<byte> span)
        {
            return Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(span));
        }

        public static ushort ToUInt16(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToUInt16();
        }
        public static ushort ToUInt16(this Memory<byte> memory)
        {
            return memory.Span.ToUInt16();
        }
        public static ushort[] ToUInt16Array(this ReadOnlySpan<byte> span)
        {
            ushort[] res = new ushort[span.Length / 2];
            int index = 0;
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = span.Slice(index, 2).ToUInt16();
                index += 2;
            }
            return res;
        }
        public static ushort[] ToUInt16Array(this ReadOnlyMemory<byte> memory)
        {
            return memory.Span.ToUInt16Array();
        }
        #endregion

        #endregion


    }
}
