using cmonitor.libs;
using cmonitor.viewer.server.win.Properties;
using common.libs;
using Microsoft.Win32;
using RDPCOMAPILib;
using System.Diagnostics;
using System.Net;
using System.Xml;

namespace cmonitor.viewer.server.win
{
    public partial class MainForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private readonly Hook hook = new Hook();
        private readonly ShareMemory shareMemory;
        private const string shareClientExe = "cmonitor.viewer.client.win";

        ParamInfo paramInfo;

        public MainForm(ParamInfo paramInfo)
        {
            this.paramInfo = paramInfo;

            InitializeComponent();
            shareMemory = new ShareMemory(paramInfo.ShareMkey, paramInfo.ShareMLength, paramInfo.ShareItemMLength);
            shareMemory.InitLocal();
        }

        private void OnLoad(object sender, EventArgs e)
        {
#if RELEASE
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
            FireWallHelper.Write(Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
#endif

            CheckRunning();

            if (paramInfo.Mode == Mode.Client)
            {
                OpenShareClient();
            }
            else
            {
                OpenShareDesktop();
            }
        }
        private void CheckRunning()
        {
            hook.Close();
            shareMemory.AddAttribute(paramInfo.ShareIndex, ShareMemoryAttribute.Running);
            shareMemory.RemoveAttribute(paramInfo.ShareIndex, ShareMemoryAttribute.Closed);
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (shareMemory.ReadAttributeEqual(paramInfo.ShareIndex, ShareMemoryAttribute.Closed))
                        {
                            CloseServer();
                        }
                        else
                        {
                            shareMemory.IncrementVersion(paramInfo.ShareIndex);
                        }
                        if (Process.GetProcessesByName(shareClientExe).Length == 0)
                        {
                            shareMemory.AddAttribute(paramInfo.ShareIndex, ShareMemoryAttribute.Error);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    await Task.Delay(30);
                }

            });
        }
        private void CloseServer()
        {
            shareMemory.RemoveAttribute(paramInfo.ShareIndex, ShareMemoryAttribute.Running);
            CloseShareClient();
            CloseShareDesktop();
            Application.ExitThread();
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }

        private void OpenShareClient()
        {
            hook.Start((code) => { return true; });

            CommandHelper.Windows(string.Empty, new string[] { $"start {shareClientExe}.exe {paramInfo.GroupName}" }, false);
        }
        private void CloseShareClient()
        {
            hook.Close();
            CommandHelper.Windows(string.Empty, new string[] { $"taskkill /f /im {shareClientExe}.exe" });
        }


        private RDPSession session;
        private NotifyIcon notifyIcon;
        private string invitationString;
        private void OpenShareDesktop()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            //notifyIcon.ContextMenuStrip.Items.Add("连接串");
            notifyIcon.ContextMenuStrip.Items.Add("刷新共享");
            notifyIcon.ContextMenuStrip.Items.Add("退出");
            notifyIcon.ContextMenuStrip.ItemClicked += (object sender, ToolStripItemClickedEventArgs e) =>
            {

                if (e.ClickedItem.Text == "退出")
                {
                    CloseServer();
                }
                else if (e.ClickedItem.Text == "刷新共享")
                {
                    NewShare();
                }
                else if (e.ClickedItem.Text == "连接串")
                {
                    //MessageBox.Show(invitationString);
                }
            };
            NewShare();
        }
        private void NewShare()
        {
            Task.Run(() =>
            {
                try
                {
                    CloseShareDesktop();
                    session = new RDPSession();
                    session.SetDesktopSharedRect(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    session.OnAttendeeConnected += Session_OnAttendeeConnected;
                    session.Open();
                    IRDPSRAPIInvitation invitation = session.Invitations.CreateInvitation(null, paramInfo.GroupName, paramInfo.GroupName, 1024);
                    invitationString = invitation.ConnectionString;

                    if(string.IsNullOrWhiteSpace(paramInfo.ProxyServers) == false)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(invitationString);

                        //留给客户端自己替换为自己本地的代理地址
                        XmlElement newLNode = xmlDoc.CreateElement("L");
                        newLNode.SetAttribute("P", "{port}");
                        newLNode.SetAttribute("N", "{ip}");
                        xmlDoc.DocumentElement["C"]["T"].AppendChild(newLNode);

                        //插入其它代理地址
                        foreach (var item in paramInfo.ProxyServers.Split(','))
                        {
                            try
                            {
                                IPEndPoint ep = IPEndPoint.Parse(item);

                                XmlElement newLNode1 = xmlDoc.CreateElement("L");
                                newLNode1.SetAttribute("P", ep.Port.ToString());
                                newLNode1.SetAttribute("N", ep.Address.ToString());
                                xmlDoc.DocumentElement["C"]["T"].AppendChild(newLNode1);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        invitationString = xmlDoc.OuterXml;
                    }

                    Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\Cmonitor", "viewerConnectStr", invitationString);

                    notifyIcon.Icon = Icon.FromHandle(Resources.logo_share_green.GetHicon());
                    notifyIcon.Text = "正在共享桌面";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex + "");
                    notifyIcon.Icon = Icon.FromHandle(Resources.logo_share_gray.GetHicon());
                    notifyIcon.Text = "共享失败";
                }
            });
        }
        private void Session_OnAttendeeConnected(object pAttendee)
        {
            IRDPSRAPIAttendee attendee = (IRDPSRAPIAttendee)pAttendee;
            attendee.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_VIEW;
        }
        private void CloseShareDesktop()
        {
            try
            {
                session?.Close();
                //Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\Cmonitor", "viewerConnectStr", string.Empty);
            }
            catch (Exception)
            {
            }
        }
    }

    public enum Mode : byte
    {
        Client = 0,
        Server = 1,
    }
}
