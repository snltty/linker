using System;

namespace linker.libs
{
    public static class VersionHelper
    {
        public static int Compare(string v1, string v2)
        {
            ReadOnlySpan<char> v1Span = v1.AsSpan();
            if (v1Span[0] == 'v') v1Span = v1Span.Slice(1);

            ReadOnlySpan<char> v2Span = v2.AsSpan();
            if (v2Span[0] == 'v') v2Span = v2Span.Slice(1);


            return Compare(Version.Parse(v1Span), Version.Parse(v2Span));
        }
        public static int Compare(Version v1, Version v2)
        {
            int v1Major = v1.Major;
            int v1Minor = v1.Minor;
            int v1Build = v1.Build;

            int v2Major = v2.Major;
            int v2Minor = v2.Minor;
            int v2Build = v2.Build;

            while (v1Major < 10000) v1Major *= 10;
            while (v2Major < 10000) v2Major *= 10;

            while (v1Minor < 1000) v1Minor *= 10;
            while (v2Minor < 1000) v2Minor *= 10;

            while (v1Build < 100) v1Build *= 10;
            while (v2Build < 100) v2Build *= 10;

            return Math.Abs((v1Major + v1Minor + v1Build) - (v2Major + v2Minor + v2Build));
        }
    }
}
