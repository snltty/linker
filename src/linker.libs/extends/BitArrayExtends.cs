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
    }
}
