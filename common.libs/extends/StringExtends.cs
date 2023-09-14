using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

namespace common.libs.extends
{
    /// <summary>
    /// |               Method |       Mean |     Error |     StdDev |     Median | Rank |   Gen0 | Allocated |
    /// |--------------------- |-----------:|----------:|-----------:|-----------:|-----:|-------:|----------:|
    /// utf8
    /// |     TestUTF8OldWrite | 121.167 ns | 0.3973 ns |  1.0467 ns | 120.881 ns |    5 | 0.0687 |     144 B |
    /// |      TestUTF8OldRead | 495.893 ns | 5.7436 ns | 15.4297 ns | 490.850 ns |    8 | 0.9174 |    1920 B |
    /// | TestUTF8OldWriteRead | 656.620 ns | 6.7862 ns | 18.1138 ns | 650.086 ns |    9 | 0.9861 |    2064 B |
    /// utf8优化
    /// |     TestUTF8NewWrite |  92.238 ns | 0.3602 ns |  0.9799 ns |  92.008 ns |    4 |      - |         - |
    /// |      TestUTF8NewRead | 135.711 ns | 0.5058 ns |  1.3412 ns | 136.129 ns |    6 | 0.1338 |     280 B |
    /// | TestUTF8NewWriteRead | 255.466 ns | 0.5967 ns |  1.6336 ns | 255.028 ns |    7 | 0.1335 |     280 B |
    /// utf16
    /// |       TestUTF16Write |   7.898 ns | 0.0723 ns |  0.2004 ns |   7.933 ns |    1 |      - |         - |
    /// |        TestUTF16Read |  19.062 ns | 0.1379 ns |  0.3682 ns |  19.032 ns |    2 | 0.0497 |     104 B |
    /// |   TestUTF16WriteRead |  25.630 ns | 0.1499 ns |  0.4102 ns |  25.524 ns |    3 | 0.0497 |     104 B |
    /// </summary>
    public static class StringExtends
    {
        /// <summary>
        /// 截取
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string SubStr(this string str, int start, int maxLength)
        {
            if (maxLength + start > str.Length)
            {
                maxLength = str.Length - start;
            }
            return str.Substring(start, maxLength);
        }
        /// <summary>
        /// md5
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Md5(this string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }


        #region utf8

        /// <summary>
        /// 慢，但通用，性能基准 1，Allocated 1
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str)
        {
            if (str == null) return Helper.EmptyArray;
            return Encoding.UTF8.GetBytes(str);
        }
        /// <summary>
        /// 慢，但通用，性能基准 1，Allocated 1
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this Span<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        /// <summary>
        /// 慢，但通用，性能基准 1，Allocated 1
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this ReadOnlySpan<byte> span)
        {
            return Encoding.UTF8.GetString(span);
        }
        /// <summary>
        /// 慢，但通用，性能基准 1，Allocated 1
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this Memory<byte> span)
        {
            return Encoding.UTF8.GetString(span.Span);
        }
        /// <summary>
        /// 慢，但通用，性能基准 1，Allocated 1
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetString(this ReadOnlyMemory<byte> span)
        {
            return Encoding.UTF8.GetString(span.Span);
        }
        #endregion

        #region utf8优化
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38
        /// utf16Length = str.AsSpan().Length
        /// 保存的时候，保存 utf16Length 和 utf8Length
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static int ToUTF8Bytes(this ReadOnlySpan<char> str, Memory<byte> bytes)
        {
            if (str.Length == 0) return 0;
            Utf8.FromUtf16(str, bytes.Span, out var _, out var utf8Length, replaceInvalidSequences: false);
            return utf8Length;
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Memory<byte> ToUTF8Bytes(this string str)
        {
            int utf16Length = 0, utf8Length = 0;
            byte[] bytes;
            int length;
            if (str != null)
            {
                var source = str.AsSpan();
                utf16Length = source.Length;
                bytes = new byte[(source.Length + 1) * 3 + 8];
                Utf8.FromUtf16(str, bytes.AsSpan(8), out _, out utf8Length, replaceInvalidSequences: false);
                length = utf8Length + 8;
            }
            else
            {
                bytes = new byte[8];
                length = 8;
            }
            utf16Length.ToBytes(bytes);
            utf8Length.ToBytes(bytes.AsMemory(4));

            return bytes.AsMemory(0, length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38
        /// </summary>
        /// <param name="span"></param>
        /// <param name="utf16Length"></param>
        /// <param name="utf8Length"></param>
        /// <returns></returns>
        public static string GetUTF8String(this Span<byte> span, int utf16Length, int utf8Length)
        {
            if (span.Length == 0) return string.Empty;
            return ReadUtf8(span, utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetUTF8String(this Span<byte> span)
        {
            if (span.Length == 0) return string.Empty;
            int utf16Length = span.ToInt32();
            int utf8Length = span.Slice(4).ToInt32();
            return ReadUtf8(span.Slice(8), utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38
        /// </summary>
        /// <param name="span"></param>
        /// <param name="utf16Length"></param>
        /// <param name="utf8Length"></param>
        /// <returns></returns>
        public static string GetUTF8String(this ReadOnlySpan<byte> span, int utf16Length, int utf8Length)
        {
            if (span.Length == 0) return string.Empty;
            return ReadUtf8(span, utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string GetUTF8String(this ReadOnlySpan<byte> span)
        {
            if (span.Length == 0) return string.Empty;
            int utf16Length = span.ToInt32();
            int utf8Length = span.Slice(4).ToInt32();
            return ReadUtf8(span.Slice(8), utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38 
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="utf16Length"></param>
        /// <param name="utf8Length"></param>
        /// <returns></returns>
        public static string GetUTF8String(this Memory<byte> memory, int utf16Length, int utf8Length)
        {
            if (memory.Length == 0) return string.Empty;
            return ReadUtf8(memory.Span, utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38  
        /// </summary>
        /// <param name="memory"></param>
        /// <returns></returns>
        public static string GetUTF8String(this Memory<byte> memory)
        {
            if (memory.Length == 0) return string.Empty;
            int utf16Length = memory.ToInt32();
            int utf8Length = memory.Slice(4).ToInt32();
            return ReadUtf8(memory.Slice(8).Span, utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38 
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="utf16Length"></param>
        /// <param name="utf8Length"></param>
        /// <returns></returns>
        public static string GetUTF8String(this ReadOnlyMemory<byte> memory, int utf16Length, int utf8Length)
        {
            if (memory.Length == 0)
                return string.Empty;
            return ReadUtf8(memory.Span, utf16Length, utf8Length);
        }
        /// <summary>
        /// UTF8比较快，但是中文字符比UTF16更大，Allocated 0.135
        /// write 0.76  read 0.27  readwrite 0.38 
        /// </summary>
        /// <param name="memory"></param>
        /// <returns></returns>
        public static string GetUTF8String(this ReadOnlyMemory<byte> memory)
        {
            if (memory.Length == 0)
                return string.Empty;
            int utf16Length = memory.ToInt32();
            int utf8Length = memory.Slice(4).ToInt32();
            return ReadUtf8(memory.Slice(8).Span, utf16Length, utf8Length);
        }
        #endregion

        #region utf16

        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="str"></param>
        /// <param name="bytes"></param>
        public static ReadOnlySpan<byte> GetUTF16Bytes(this string str)
        {
            if (str == null) return Helper.EmptyArray;
            return MemoryMarshal.AsBytes(str.AsSpan());
        }

        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] ToUTF16Bytes(this string str)
        {
            byte[] bytes;

            if (str != null)
            {
                var source = MemoryMarshal.AsBytes(str.AsSpan());
                bytes = new byte[source.Length + 4];
                source.CopyTo(bytes.AsSpan(4));
            }
            else
            {
                bytes = new byte[4];
            }
            str.Length.ToBytes(bytes);

            return bytes;
        }

        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="span"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this Span<byte> span, int strLength)
        {
            if (span.Length == 0) return string.Empty;
            return ReadUtf16(span, strLength);
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="span"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this Span<byte> span)
        {
            if (span.Length == 0) return string.Empty;
            int strLength = span.ToInt32();
            return ReadUtf16(span.Slice(4), strLength);
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="span"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this ReadOnlySpan<byte> span, int strLength)
        {
            if (span.Length == 0) return string.Empty;
            return ReadUtf16(span, strLength);
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="span"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this ReadOnlySpan<byte> span)
        {
            if (span.Length == 0)
                return string.Empty;
            int strLength = span.ToInt32();
            return ReadUtf16(span.Slice(4), strLength);
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039 
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this Memory<byte> memory, int strLength)
        {
            if (memory.Length == 0)
                return string.Empty;    
            return ReadUtf16(memory.Span, strLength);
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039 
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this Memory<byte> memory)
        {
            if (memory.Length == 0) return string.Empty;
            return memory.Span.GetUTF16String();
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this ReadOnlyMemory<byte> memory, int strLength)
        {
            if (memory.Length == 0)
                return string.Empty;
            return ReadUtf16(memory.Span, strLength);
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this ReadOnlyMemory<byte> memory)
        {
            if (memory.Length == 0) return string.Empty;
            return memory.Span.GetUTF16String();
        }
        /// <summary>
        /// utf16非常快，但是，ASCII 字符的大小将是原来的两倍，中文字符则比UTF8略小，Allocated 0.05
        /// write  0.065  read 0.038   readwrite 0.039
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="strLength"></param>
        /// <returns></returns>
        public static string GetUTF16String(this byte[] memory, int strLength)
        {
            if (memory.Length == 0) return string.Empty;
            return ReadUtf16(memory, strLength);
        }


        #endregion

        static string ReadUtf8(ReadOnlySpan<byte> span, int utf16Length, int utf8Length)
        {
            unsafe
            {
                fixed (byte* p = &span[0])
                {
                    return string.Create(utf16Length, ((IntPtr)p, utf8Length), static (dest, state) =>
                    {
                        var src = MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>((byte*)state.Item1), state.Item2);
                        Utf8.ToUtf16(src, dest, out var bytesRead, out var charsWritten, replaceInvalidSequences: false);
                    });
                }
            }
        }
        static string ReadUtf16(ReadOnlySpan<byte> span, int strLength)
        {
            ReadOnlySpan<char> src = MemoryMarshal.Cast<byte, char>(span).Slice(0, strLength);
            return new string(src);
        }

    }
}
