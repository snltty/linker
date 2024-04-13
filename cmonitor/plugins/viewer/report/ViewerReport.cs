using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using cmonitor.plugins.viewer.messenger;
using cmonitor.server;
using common.libs;
using MemoryPack;
using cmonitor.client.running;
using System.Net;
using System.Text.Json;
using cmonitor.plugins.viewer.proxy;

namespace cmonitor.plugins.viewer.report
{
    /// <summary>
    /// 1、通知客户端A，开启共享服务端，获取connectStr
    /// 2、客户端A，通知其它客户端，开启共享客户端
    /// 3、客户端A定时发送心跳，其它客户端收到心跳，更新connectStr，如果未运行，或者运行的不是共享客户端，则重新运行
    /// </summary>
    public sealed class ViewerReport : IClientReport
    {
        public string Name => "Viewer";

        private ViewerReportInfo report = new ViewerReportInfo();
        private readonly RunningConfig runningConfig;
        private readonly IViewer viewer;
        private readonly ShareMemory shareMemory;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly Config config;
        private readonly ViewerProxyClient viewerProxyClient;

        public ViewerReport(Config config, RunningConfig runningConfig, IViewer viewer, ShareMemory shareMemory, ClientSignInState clientSignInState, MessengerSender messengerSender, ViewerProxyClient viewerProxyClient)
        {
            this.config = config;
            this.runningConfig = runningConfig;
            this.viewer = viewer;
            this.shareMemory = shareMemory;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;
            this.viewerProxyClient = viewerProxyClient;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Update();
                ViewerTask();
            };
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = Running();
            report.Mode = runningConfig.Data.Viewer.Mode;
            report.ShareId = runningConfig.Data.Viewer.ShareId;
            if (reportType == ReportType.Full || report.Updated() || shareMemory.ReadVersionUpdated((int)ShareMemoryIndexs.Viewer))
            {
                return report;
            }
            return null;
        }

        public void Server(ViewerRunningConfigInfo info)
        {
            if (info.Open)
            {
                runningConfig.Data.Viewer = info;
            }
            else
            {
                runningConfig.Data.Viewer.Open = info.Open;
            }
            runningConfig.Data.Update();


            Task.Run(async () =>
            {
                Close();
                await HeartNotify(false);
                await Task.Delay(500);
                Open();

                if (runningConfig.Data.Viewer.Open)
                {
                    runningConfig.Data.Viewer.ConnectStr = await GetNewConnectStr();
                    if (string.IsNullOrWhiteSpace(runningConfig.Data.Viewer.ConnectStr) == false)
                    {
                        UpdateConnectEP();
                        await HeartNotify(runningConfig.Data.Viewer.Open);
                    }
                }
            });
        }
        public void Heart(ViewerRunningConfigInfo info)
        {
            if (info.ConnectStr != runningConfig.Data.Viewer.ConnectStr)
            {
                viewer.SetConnectString(ReplaceProxy(info.ConnectStr));
            }

            //未运行，或者不是client模式，或者状态不对，都需要重启一下
            bool restart = Running() != true
                || runningConfig.Data.Viewer.Mode != ViewerMode.Client
                || runningConfig.Data.Viewer.Open != info.Open;

            runningConfig.Data.Viewer = info;
            runningConfig.Data.Update();

            if (restart)
            {
                RestartClient();
            }
        }
        private async Task HeartNotify(bool open)
        {
            ViewerRunningConfigInfo info = JsonSerializer.Deserialize<ViewerRunningConfigInfo>(JsonSerializer.Serialize(runningConfig.Data.Viewer));
            info.Mode = ViewerMode.Client;
            info.Open = open;

            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ViewerMessengerIds.HeartNotify,
                Payload = MemoryPackSerializer.Serialize(info)
            });
        }

        private void RestartClient()
        {
            Task.Run(async () =>
            {
                Close();
                await Task.Delay(500);
                Open();
            });
        }
        private void Update()
        {
            if (runningConfig.Data.Viewer.Mode == ViewerMode.Server && runningConfig.Data.Viewer.Open && Running() == false)
            {
                Server(runningConfig.Data.Viewer);
            }
        }


        private async Task<string> GetNewConnectStr()
        {
            try
            {
                for (int i = 0; i < 300; i++)
                {
                    if (shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Running))
                    {
                        string connectStr = viewer.GetConnectString();
                        if (string.IsNullOrWhiteSpace(connectStr) == false) return connectStr;
                    }
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return string.Empty;
        }
        private string ReplaceProxy(string connectStr)
        {
            if (IPAddress.IsLoopback(clientSignInState.Connection.LocalAddress.Address))
            {
                Logger.Instance.Warning($"use Loopback address【{clientSignInState.Connection.LocalAddress.Address}】 as  viewer proxy，may fail");
                Logger.Instance.Warning($"connect to the local or external network address of the cmonitor server,to fix this");
            }

            return connectStr
                .Replace("{ip}", clientSignInState.Connection.LocalAddress.Address.ToString())
            //.Replace("{port}", "12345");
                .Replace("{port}", viewerProxyClient.LocalEndpoint.Port.ToString());

        }
        private void UpdateConnectEP()
        {
            string connectEP = viewer.GetConnectEP(runningConfig.Data.Viewer.ConnectStr);
            runningConfig.Data.Viewer.ConnectEP = connectEP;
            runningConfig.Data.Update();
        }

        private void Open()
        {
            if (runningConfig.Data.Viewer.Open)
            {
                viewer.Open(runningConfig.Data.Viewer.Open, new ParamInfo
                {
                    GroupName = runningConfig.Data.Viewer.ShareId,
                    Mode = runningConfig.Data.Viewer.Mode,
                    ProxyServers = string.Join(",", new string[] {
                           $"{clientSignInState.Connection.Address.Address}:{config.Data.Client.Viewer.ProxyPort}"
                    }),
                    ShareIndex = (int)ShareMemoryIndexs.Viewer,
                    ShareMkey = config.Data.Client.ShareMemoryKey,
                    ShareMLength = config.Data.Client.ShareMemoryCount,
                    ShareItemMLength = config.Data.Client.ShareMemorySize
                });
            }
        }
        private void Close()
        {
            shareMemory.AddAttribute((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Closed);
            shareMemory.RemoveAttribute((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Running);
            runningConfig.Data.Viewer.ConnectStr = string.Empty;
            viewer.SetConnectString(runningConfig.Data.Viewer.ConnectStr);
            runningConfig.Data.Update();
        }

        private bool Running()
        {
            long version = shareMemory.ReadVersion((int)ShareMemoryIndexs.Viewer);
            return shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Running)
                    && Helper.Timestamp() - version < 1000;
        }
        private void ViewerTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    bool heart = string.IsNullOrWhiteSpace(runningConfig.Data.Viewer.ConnectStr) == false
                     && runningConfig.Data.Viewer.Open && runningConfig.Data.Viewer.Mode == ViewerMode.Server && Running();

                    if (heart)
                    {
                        await HeartNotify(runningConfig.Data.Viewer.Open);
                    }

                    await Task.Delay(5000);
                }
            });
        }
    }

    [MemoryPackable]
    public sealed partial class ViewerHeartInfo
    {
        public string Server { get; set; }
        public string ConnectStr { get; set; }
    }

    public sealed class ViewerReportInfo : ReportInfo
    {
        public bool Value { get; set; }
        public ViewerMode Mode { get; set; }
        public string ShareId { get; set; } = string.Empty;
        public override int HashCode()
        {
            return Value.GetHashCode() ^ Mode.GetHashCode() ^ ShareId.GetHashCode();
        }
    }
}
