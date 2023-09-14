using cmonitor.server.service.messengers.sign;
using cmonitor.server.service;
using common.libs;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using common.libs.extends;
using System.Reflection;

namespace cmonitor.server.client.reports
{
    public interface IReport
    {
        public string Name { get; }

        public Dictionary<string, object> GetReports();
    }

    public sealed class ReportTransfer : IReport
    {
        public string Name => string.Empty;

        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly ServiceProvider serviceProvider;
        private readonly Config config;

        private List<IReport> reports;
        private Dictionary<string, Dictionary<string, object>> reportObj;
        public ReportTransfer(ClientSignInState clientSignInState, MessengerSender messengerSender, ServiceProvider serviceProvider, Config config)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.serviceProvider = serviceProvider;
            this.config = config;

            if (config.IsCLient)
            {
                ReportTask();
            }

        }

        public Dictionary<string, object> GetReports()
        {
            return new Dictionary<string, object>();
        }

        public void LoadPlugins(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(IReport));
            reports = types.Select(c => (IReport)serviceProvider.GetService(c)).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            reportObj = new Dictionary<string, Dictionary<string, object>>(reports.Count);
        }
        private uint reportFlag = 0;
        public void Update()
        {
            Interlocked.CompareExchange(ref reportFlag, 1, 0);
        }
        private void ReportTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (clientSignInState.Connected == true && Interlocked.CompareExchange(ref reportFlag, 0, 1) == 1)
                        {
                            await SendReport();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Error(ex);
                    }
                    await Task.Delay(Config.ReportTime);
                }
            }, TaskCreationOptions.LongRunning);
        }
        private async Task SendReport()
        {
            reportObj.Clear();
            foreach (IReport item in reports)
            {
                if (string.IsNullOrWhiteSpace(item.Name) == false)
                {
                    reportObj.Add(item.Name, item.GetReports());
                }
            }
            byte[] res = MemoryPackSerializer.Serialize(reportObj.ToJson());
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ReportMessengerIds.Report,
                Payload = res
            });
        }
    }
}
