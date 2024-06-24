using Linker.Libs;

namespace Linker.Config
{
    public partial class ConfigClientInfo
    {
        public CApiConfigClientInfo CApi { get; set; } = new CApiConfigClientInfo();
    }

    public sealed class CApiConfigClientInfo
    {
        public int ApiPort { get; set; } = 1803;
        public string ApiPassword { get; set; } = Helper.GlobalString;

        public int WebPort { get; set; } = 1804;
        public string WebRoot { get; set; } = "./web/";
    }
}
