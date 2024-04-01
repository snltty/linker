using cmonitor.client.runningConfig;
using cmonitor.client.report;
using cmonitor.client.ruleConfig;
using cmonitor.config;
using cmonitor.libs;
using cmonitor.startup;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.client
{
    public sealed class ClientStartup : IStartup
    {
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RuleConfig>();

            serviceCollection.AddSingleton<ClientReportTransfer>();

            serviceCollection.AddSingleton<ClientSignInState>();
            serviceCollection.AddSingleton<ClientSignInTransfer>();


            if (OperatingSystem.IsWindows()) serviceCollection.AddSingleton<IRunningConfig, RunningConfigWindows>();
            else if (OperatingSystem.IsLinux()) serviceCollection.AddSingleton<IRunningConfig, RunningConfigLinux>();
            else if (OperatingSystem.IsMacOS()) serviceCollection.AddSingleton<IRunningConfig, RunningConfigMacOS>();

            //内存共享
            ShareMemory shareMemory = new ShareMemory(config.Client.ShareMemoryKey, config.Client.ShareMemoryCount, config.Client.ShareMemorySize);
            serviceCollection.AddSingleton<ShareMemory>((a) => shareMemory);
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<RuleConfig>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
            Logger.Instance.Info($"start client");
            Logger.Instance.Info($"server ip {config.Server}");

            Logger.Instance.Info($"start client report transfer");
            ClientReportTransfer report = serviceProvider.GetService<ClientReportTransfer>();
            report.LoadPlugins(assemblies);

            Logger.Instance.Info($"start client share memory");
            ShareMemory shareMemory = serviceProvider.GetService<ShareMemory>();
            shareMemory.InitLocal();
            shareMemory.InitGlobal();
            shareMemory.StartLoop();

            Logger.Instance.Info($"start client signin transfer");
            ClientSignInTransfer clientTransfer = serviceProvider.GetService<ClientSignInTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
