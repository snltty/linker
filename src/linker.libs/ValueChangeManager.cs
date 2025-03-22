namespace linker.libs
{
    public sealed class StringChangedManager
    {
        string _value;

        public bool Input(string value)
        {
            bool changed = _value != value;
            _value = value;
            return changed;
        }
    }
}
