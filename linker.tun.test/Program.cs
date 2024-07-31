using System.Net;

namespace linker.tun.test
{
    internal class Program
    {
        static void Main(string[] args)
        {

            LinkerWinTunDevice linkerWinTunDevice = new LinkerWinTunDevice("linker111", Guid.Parse("d9f71e4f-ba49-4cba-be5e-69a5694df8cb"));
            linkerWinTunDevice.SetUp(IPAddress.Parse("192.168.55.2"), IPAddress.Parse("192.168.55.1"), 24, out string error);

            Console.WriteLine(error);

            Console.ReadLine();
        }
    }
}
