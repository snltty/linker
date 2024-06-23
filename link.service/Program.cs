using System.ServiceProcess;

namespace link.service
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
                    new LinkService(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
