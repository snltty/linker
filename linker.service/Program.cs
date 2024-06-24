using System.ServiceProcess;

namespace linker.service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (a, b) =>
            {
            };

            if (OperatingSystem.IsWindows())
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new LinkerService(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
