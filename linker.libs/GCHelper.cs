﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace linker.libs
{
    public static class GCHelper
    {
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        public static void FlushMemory()
        {
            try
            {
                GC.RefreshMemoryLimit();
            }
            catch (Exception)
            {
            }

            GC.Collect();
            GC.Collect(2, GCCollectionMode.Aggressive);

            GC.SuppressFinalize(true);
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
        public static void Gc(object obj)
        {
            GC.Collect();
            GC.SuppressFinalize(obj);
        }
    }
}
