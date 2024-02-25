using System.Runtime.InteropServices;

namespace common.libs.winapis
{
    public static class Wininet
    {
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int description, int reservedValue);
    }
}
