using System.ServiceProcess;

namespace cmonitor.network.service
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
                    new CmonitorNetworkService(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
