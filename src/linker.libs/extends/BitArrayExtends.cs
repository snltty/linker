using System.Collections;
namespace linker.libs.extends
{
    public static class BitArrayExtends
    {
        public static BitArray PadRight(this BitArray bits, int newLength, bool value)
        {
            if (bits.Count == newLength) return bits;

            bool[] newArray = new bool[newLength];
            bits.CopyTo(newArray, 0);
            for (int i = bits.Count; i < newLength; i++)
            {
                newArray[i] = value;
            }
            return new BitArray(newArray);
        }
        public static string ToBinaryStringFast(this BitArray bits)
        {
            int length = bits.Length;
            if (length == 0) return string.Empty;

            return string.Create(length, bits, (span, array) =>
            {
                for (int i = 0; i < array.Length; i++)
                {
                    span[i] = array[i] ? '1' : '0';
                }
            });
        }
    }
}
