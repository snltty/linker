namespace Linker.Libs
{
    public static class BooleanHelper
    {
        public static bool CompareExchange(ref bool location, bool value, bool comparand)
        {
            bool result = location;

            if(location == comparand)
            {
                location = value;
            }
            return result;
        }
    }
}
