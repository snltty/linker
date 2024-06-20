using System.ServiceProcess;

namespace cmonitor.sas.service
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
                new CmonitorSasService(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
