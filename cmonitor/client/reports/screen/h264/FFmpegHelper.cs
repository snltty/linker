using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;

namespace cmonitor.client.reports.screen.h264;

internal static class FFmpegHelper
{
    public static unsafe string av_strerror(int error)
    {
        var bufferSize = 1024;
        var buffer = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
        var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
        return message;
    }

    public static int ThrowExceptionIfError(this int error)
    {

        if (error < 0) throw new ApplicationException(av_strerror(error));
        return error;
    }

    public static void Initialize()
    {
        RegisterFFmpegBinaries();
        FFmpeg.AutoGen.Bindings.DynamicallyLoaded.DynamicallyLoadedBindings.Initialize();
    }
    internal static void RegisterFFmpegBinaries()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var current = Environment.CurrentDirectory;
            while (current != null)
            {
                var ffmpegBinaryPath = Path.Combine(current, "ffmpeg");

                if (Directory.Exists(ffmpegBinaryPath))
                {
                    //Console.WriteLine($"FFmpeg binaries found in: {ffmpegBinaryPath}");
                    FFmpeg.AutoGen.Bindings.DynamicallyLoaded.DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaryPath;
                    return;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }
        else
            throw new NotSupportedException(); // fell free add support for platform of your choose
    }
}


