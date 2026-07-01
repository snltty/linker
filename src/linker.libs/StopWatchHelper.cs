using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace linker.libs
{
    public static class StopWatchHelper
    {
        private static long[] starts = new long[255];
        private static double[] ends = new double[255];

        private const bool ENABLED = false;

        static StopWatchHelper()
        {
            if (ENABLED)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var times = Enumerable.Range(0, 12).Select(c => $"{(StopWatchType)c}->{ends[c]}");
                        Console.WriteLine(string.Join("\n", times));
                        Console.WriteLine($"============================================================");

                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StartTimestamp(StopWatchType type)
        {
            if (ENABLED)
                starts[(byte)type] = Stopwatch.GetTimestamp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EndTimestamp(StopWatchType type)
        {
            if (ENABLED)
                ends[(byte)type] = (Stopwatch.GetElapsedTime(starts[(byte)type], Stopwatch.GetTimestamp()).TotalNanoseconds);
        }

        public enum StopWatchType : byte
        {
            Tun_Read_Unpacket = 0,
            Tun_Read_Hook = 1,
            Tun_Read_Callback = 2,
            Tun_Read_Connecttion = 3,
            Tun_Read_Send = 4,

            Udp_Lock = 5,
            Udp_Encode = 6,
            Udp_Send = 7,

            Tun_Write_Hook = 8,
            Tun_Write = 9,

            Tun_Read_Write = 10,
            Tun_Write_Read = 11,

        }
    }
}
