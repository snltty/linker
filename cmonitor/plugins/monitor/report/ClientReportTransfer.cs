using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.plugins.report.messenger;
using cmonitor.server;
using common.libs;
using common.libs.extends;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.report
{
    public sealed class ClientReportTransfer
    {
        public string Name => string.Empty;

        private readonly ClientSignInState clientSignInState;
        private readonly MessengerSender messengerSender;
        private readonly ServiceProvider serviceProvider;
        private readonly Config config;

        private List<IClientReport> reports;
        private Dictionary<string, object> reportObj;
        private ReportType reportType = ReportType.Full | ReportType.Trim;

        public ClientReportTransfer(ClientSignInState clientSignInState, MessengerSender messengerSender, ServiceProvider serviceProvider, Config config)
        {
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.serviceProvider = serviceProvider;
            this.config = config;
            ReportTask();
        }

        public void LoadPlugins(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(IClientReport));
            reports = types.Select(c => (IClientReport)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            reportObj = new Dictionary<string, object>(reports.Count);

            Logger.Instance.Warning($"load reports:{string.Join(",", reports.Select(c => c.Name))}");
        }

        private uint reportFlag = 0;
        private long ticks = 0;
        public void Update(ReportType reportType)
        {
            this.reportType |= reportType;
            ticks = DateTime.UtcNow.Ticks;
            Interlocked.CompareExchange(ref reportFlag, 1, 0);
        }

        private void ReportTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        bool res = clientSignInState.Connected == true
                        && (Interlocked.CompareExchange(ref reportFlag, 0, 1) == 1 || (DateTime.UtcNow.Ticks - ticks) / TimeSpan.TicksPerMillisecond < 1000);
                        if (res)
                        {
                            await SendReport();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Error(ex);
                    }
                    await Task.Delay(30);
                }
            });
        }
        private async Task SendReport()
        {
            reportObj.Clear();

            foreach (IClientReport item in reports)
            {
                if (string.IsNullOrWhiteSpace(item.Name) == false)
                {
                    object val = item.GetReports(reportType & ReportType.Full);
                    if (val != null)
                    {
                        reportObj[item.Name] = val;
                    }
                }
            }
            reportType &= ~reportType;

            if (reportObj.Count > 0)
            {

                string json = reportObj.ToJson();
                byte[] res = MemoryPackSerializer.Serialize(json);
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ReportMessengerIds.Report,
                    Payload = res
                });
            }
        }
    }
}
