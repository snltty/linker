using cmonitor.client;
using cmonitor.client.runningConfig;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using cmonitor.plugins.viewer.messenger;
using cmonitor.server;
using common.libs;
using MemoryPack;

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
        private readonly IRunningConfig clientConfig;
        private readonly IViewer viewer;
        private readonly ShareMemory shareMemory;
        private ViewerConfigInfo viewerConfigInfo;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public ViewerReport(Config config, IRunningConfig clientConfig, IViewer viewer, ShareMemory shareMemory, ClientSignInState clientSignInState, MessengerSender messengerSender)
        {
            this.clientConfig = clientConfig;
            this.viewer = viewer;
            this.shareMemory = shareMemory;
            this.clientSignInState = clientSignInState;
            this.messengerSender = messengerSender;

            viewerConfigInfo = clientConfig.Get(new ViewerConfigInfo { });
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                Update();
                ViewerTask();
            };
        }

        public object GetReports(ReportType reportType)
        {
            report.Value = Running();
            report.Mode = viewerConfigInfo.Mode;
            if (reportType == ReportType.Full || report.Updated() || shareMemory.ReadVersionUpdated((int)ShareMemoryIndexs.Viewer))
            {
                return report;
            }
            return null;
        }

        private void Update()
        {
            if (viewerConfigInfo.Mode == ViewerMode.Server && viewerConfigInfo.Open && Running() == false)
            {
                Server(viewerConfigInfo);
            }
        }
        public void Server(ViewerConfigInfo info)
        {
            if (info.Open)
            {
                viewerConfigInfo = info;
            }
            else
            {
                viewerConfigInfo.Open = info.Open;
            }

            clientConfig.Set(viewerConfigInfo);
            viewerConfigInfo.ConnectStr = string.Empty;
            viewer.SetConnectString(viewerConfigInfo.ConnectStr);

            Task.Run(async () =>
            {
                shareMemory.AddAttribute((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Closed);
                shareMemory.RemoveAttribute((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Running);
                await Task.Delay(200);

                viewer.Open(viewerConfigInfo.Open, viewerConfigInfo.Mode);
                if (viewerConfigInfo.Open)
                {
                    try
                    {
                        for (int i = 0; i < 300; i++)
                        {
                            var attr = shareMemory.ReadAttribute((int)ShareMemoryIndexs.Viewer);
                            if (shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Running))
                            {
                                viewerConfigInfo.ConnectStr = viewer.GetConnectString();
                                if (string.IsNullOrWhiteSpace(viewerConfigInfo.ConnectStr) == false)
                                {
                                    NotifyHeart();
                                    break;
                                }
                            }
                            await Task.Delay(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
                else
                {
                    NotifyClient();
                }
            });
        }

        private void RestartClient()
        {
            Task.Run(async () =>
            {
                shareMemory.AddAttribute((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Closed);
                shareMemory.RemoveAttribute((int)ShareMemoryIndexs.Viewer, ShareMemoryAttribute.Running);
                await Task.Delay(500);
                viewer.Open(viewerConfigInfo.Open, viewerConfigInfo.Mode);
            });
        }
        public void Client(ViewerConfigInfo info)
        {
            viewerConfigInfo = info;
            clientConfig.Set(viewerConfigInfo);
            viewer.SetConnectString(viewerConfigInfo.ConnectStr);
            RestartClient();
        }
        private void NotifyClient()
        {
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ViewerMessengerIds.NotifyClient,
                Payload = MemoryPackSerializer.Serialize(viewerConfigInfo)
            });
        }
        public void Heart(string connectStr)
        {
            bool restart = Running() == false || viewerConfigInfo.Mode == ViewerMode.Server;
            viewerConfigInfo.ConnectStr = connectStr;
            viewerConfigInfo.Mode = ViewerMode.Client;
            viewerConfigInfo.Open = true;
            clientConfig.Set(viewerConfigInfo);
            viewer.SetConnectString(viewerConfigInfo.ConnectStr);

            if (restart)
            {
                RestartClient();
            }
        }
        private void NotifyHeart()
        {
            if (string.IsNullOrWhiteSpace(viewerConfigInfo.ConnectStr))
            {
                return;
            }
            if (viewerConfigInfo.Open && viewerConfigInfo.Mode == ViewerMode.Server && Running())
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ViewerMessengerIds.NotifyHeart,
                    Payload = MemoryPackSerializer.Serialize(viewerConfigInfo)
                });
            }
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
                    NotifyHeart();
                    await Task.Delay(5000);
                }
            });
        }
    }

    public sealed class ViewerReportInfo : ReportInfo
    {
        public bool Value { get; set; }
        public ViewerMode Mode { get; set; }
        public override int HashCode()
        {
            return Value.GetHashCode() ^ Mode.GetHashCode();
        }
    }
}
