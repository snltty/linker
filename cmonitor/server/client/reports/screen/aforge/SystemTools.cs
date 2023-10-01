using System;
using System.Runtime.InteropServices;

namespace cmonitor.server.client.reports.screen.aforge
{

    public static class SystemTools
    {
        public static IntPtr CopyUnmanagedMemory(IntPtr dst, IntPtr src, int count)
        {
            unsafe
            {
                CopyUnmanagedMemory((byte*)dst.ToPointer(), (byte*)src.ToPointer(), count);
            }
            return dst;
        }
        public static unsafe byte* CopyUnmanagedMemory(byte* dst, byte* src, int count)
        {
#if !MONO
            return memcpy(dst, src, count);
#else
            int countUint = count >> 2;
            int countByte = count & 3;

            uint* dstUint = (uint*) dst;
            uint* srcUint = (uint*) src;

            while ( countUint-- != 0 )
            {
                *dstUint++ = *srcUint++;
            }

            byte* dstByte = (byte*) dstUint;
            byte* srcByte = (byte*) srcUint;

            while ( countByte-- != 0 )
            {
                *dstByte++ = *srcByte++;
            }
            return dst;
#endif
        }

        public static IntPtr SetUnmanagedMemory(IntPtr dst, int filler, int count)
        {
            unsafe
            {
                SetUnmanagedMemory((byte*)dst.ToPointer(), filler, count);
            }
            return dst;
        }
        public static unsafe byte* SetUnmanagedMemory(byte* dst, int filler, int count)
        {
#if !MONO
            return memset(dst, filler, count);
#else
            int countUint = count >> 2;
            int countByte = count & 3;

            byte fillerByte = (byte) filler;
            uint fiilerUint = (uint) filler | ( (uint) filler << 8 ) |
                                              ( (uint) filler << 16 );// |
                                              //( (uint) filler << 24 );

            uint* dstUint = (uint*) dst;

            while ( countUint-- != 0 )
            {
                *dstUint++ = fiilerUint;
            }

            byte* dstByte = (byte*) dstUint;

            while ( countByte-- != 0 )
            {
                *dstByte++ = fillerByte;
            }
            return dst;
#endif
        }


#if !MONO
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern byte* memcpy(
            byte* dst,
            byte* src,
            int count);

        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern byte* memset(
            byte* dst,
            int filler,
            int count);
#endif
    }
}
