using System;
using System.Linq;
using System.Reflection;

namespace linker.libs
{
    public static class VersionHelper
    {
        static string version = $"v{string.Join(".", Assembly.GetExecutingAssembly().GetName().Version.ToString().Split('.').Take(3))}";
        public static string Version => version;

        /// <summary>
        /// 比较版本，相差多少
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="abs">是否取绝对值</param>
        /// <returns>负整数 v1小于v2多少，0相等，正整数v1大于v2多少</returns>
        public static int Compare(string v1, string v2, bool abs)
        {
            ReadOnlySpan<char> v1Span = v1.AsSpan();
            if (v1Span[0] == 'v') v1Span = v1Span.Slice(1);

            ReadOnlySpan<char> v2Span = v2.AsSpan();
            if (v2Span[0] == 'v') v2Span = v2Span.Slice(1);


            return Compare(System.Version.Parse(v1Span), System.Version.Parse(v2Span), abs);
        }
        /// <summary>
        /// 比较版本，相差多少
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="abs">是否取绝对值</param>
        /// <returns>负整数 v1小于v2多少，0相等，正整数v1大于v2多少</returns>
        public static int Compare(Version v1, Version v2, bool abs)
        {
            int v1Major = v1.Major;
            int v1Minor = v1.Minor;
            int v1Build = v1.Build;

            int v2Major = v2.Major;
            int v2Minor = v2.Minor;
            int v2Build = v2.Build;

            while (v1Major < 10000 && v1Major > 0) v1Major *= 10;
            while (v2Major < 10000 && v2Major > 0) v2Major *= 10;

            while (v1Minor < 1000 && v1Minor > 0) v1Minor *= 10;
            while (v2Minor < 1000 && v2Minor > 0) v2Minor *= 10;

            while (v1Build < 100 && v1Build > 0) v1Build *= 10;
            while (v2Build < 100 && v2Build > 0) v2Build *= 10;

            int value = (v1Major + v1Minor + v1Build) - (v2Major + v2Minor + v2Build);
            if (abs)
            {
                value = Math.Abs(value);
            }

            return value;
        }
    }
}
