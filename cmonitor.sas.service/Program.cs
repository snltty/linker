using System.ServiceProcess;

namespace cmonitor.sas.service
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main(string[] args)
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
