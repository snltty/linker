using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using Java.IO;
using linker.app.Services;
using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.entry;
using linker.messenger.tuntap;
using linker.messenger.updater;
using linker.nat;
using linker.tun.device;
using linker.tunnel.connection;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace linker.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        Intent intent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetLightStatusBar();

            base.OnCreate(savedInstanceState);

            intent = new Intent(this, typeof(ForegroundService));
            ContextCompat.StartForegroundService(this,intent);
        }
        public void SetLightStatusBar()
        {
            Window.SetStatusBarColor(Android.Graphics.Color.Rgb(255, 255, 255));
            Window.SetNavigationBarColor(Android.Graphics.Color.Rgb(255, 255, 255));

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
            {
                WindowCompat.GetInsetsController(Window, Window.DecorView).AppearanceLightStatusBars = true;
            }
            else
            {
                Window.DecorView.SystemUiFlags = SystemUiFlags.LightStatusBar;
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }
        protected override void OnStart()
        {
            base.OnStart();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

    /// <summary>
    /// VPN服务，先启动服务，才能创建VPN连接
    /// </summary>
    [Service(Label = "VpnServiceLinker", Name = "com.snltty.linker.app.VpnServiceLinker", Enabled = true, Permission = "android.permission.BIND_VPN_SERVICE")]
    public class VpnServiceLinker : VpnService, ILinkerTunDeviceCallback, ITuntapProxyCallback
    {
        private static LinkerVpnDevice linkerVpnDevice = new LinkerVpnDevice();

        TuntapConfigTransfer tuntapConfigTransfer;
        TuntapProxy tuntapProxy;
        TuntapDecenter tuntapDecenter;
        TuntapTransfer tuntapTransfer;
        public VpnServiceLinker()
        {
            tuntapTransfer = LinkerMessengerEntry.GetService<TuntapTransfer>();

            tuntapConfigTransfer = LinkerMessengerEntry.GetService<TuntapConfigTransfer>();
            tuntapProxy = LinkerMessengerEntry.GetService<TuntapProxy>();
            tuntapProxy.Callback = this;
            tuntapDecenter = LinkerMessengerEntry.GetService<TuntapDecenter>();

            linkerVpnDevice.SetVpnService(this);
            tuntapTransfer.Initialize(linkerVpnDevice, this);
        }
        public override void OnCreate()
        {
            base.OnCreate();
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            tuntapTransfer.Shutdown();
        }

        public async Task Callback(LinkerTunDevicPacket packet)
        {
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }
        public async Task Callback(LinkerSrcProxyReadPacket packet)
        {
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }
        public bool Callback(uint ip)
        {
            return tuntapProxy.TestIp(ip);
        }
        public async ValueTask Close(ITunnelConnection connection)
        {
            //tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
        public async ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer)
        {
           await tuntapTransfer.Write(connection.RemoteMachineId, buffer).ConfigureAwait(false);
        }

    }

    /// <summary>
    /// 前台服务，运行Linker
    /// </summary>
    [Service(Label = "ForegroundService", Name = "com.snltty.linker.app.ForegroundService", Exported = true)]
    [IntentFilter(new string[] { "com.snltty.linker.app.ForegroundService" })]
    public sealed class ForegroundService : Service
    {
        private static readonly int SERVICE_ID = 10000;
        private static readonly string CHANNEL_ID = "linker.app";
        private static readonly string CHANNEL_NAME = "linker.app";
        private static readonly string CHANNEL_CONTENT = "linker.app are running";

        Intent intent;
        Intent vpnIntent;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            RunLinker();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            intent.SetFlags(ActivityFlags.NewTask);
            this.intent = intent;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel notificationChannel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High);
                notificationChannel.EnableLights(true);
                notificationChannel.SetShowBadge(true);
                notificationChannel.LockscreenVisibility = NotificationVisibility.Public;
                NotificationManager manager = (NotificationManager)GetSystemService(NotificationService);
                manager.CreateNotificationChannel(notificationChannel);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(SERVICE_ID, CreateNotification(), Android.Content.PM.ForegroundService.TypeDataSync);
            }
            else
            {
                StartForeground(SERVICE_ID, CreateNotification());
            }

            return StartCommandResult.Sticky;
        }
        private Notification CreateNotification()
        {
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Mutable);
            Notification notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.logo)
                .SetChannelId(CHANNEL_ID)
                .SetContentIntent(pendingIntent)
                .SetContentTitle(CHANNEL_NAME)
                .SetContentText(CHANNEL_CONTENT)
                .SetOngoing(true).SetPriority(0)
                .Build();
            notification.Flags |= NotificationFlags.NoClear;
            return notification;
        }

        private void RunLinker()
        {
            Helper.SetCurrentDirectory(FileSystem.Current.AppDataDirectory);

            InitLogger();

            LinkerMessengerEntry.Initialize();
            LinkerMessengerEntry.AddService<IWebServerFileReader, WebServerFileReader>();
            LinkerMessengerEntry.AddService<ITuntapSystemInformation, SystemInformation>();
            LinkerMessengerEntry.AddService<IUpdaterInstaller, UpdaterInstaller>();

            LinkerMessengerEntry.Build();

            using JsonDocument config = InitConfig();
            LinkerMessengerEntry.Setup(ExcludeModule.None, config);
            IPlatformApplication.Current.Services.GetService<InitializeService>().SendOnInitialized();

            TuntapSetup();

        }
        private void InitLogger()
        {
            LoggerHelper.Instance.OnLogger += (model) =>
            {
                string line = $"[{model.Type,-7}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}";
                Android.Util.Log.Debug("linker", line);
            };
        }
        private JsonDocument InitConfig()
        {
            return JsonDocument.Parse(new
            {
                Common = new { Modes = new string[] { "client" } },
                Client = new
                {
                    CApi = new
                    {
                        ApiPassword = Helper.GlobalString,
                        WebPort = NetworkHelper.ApplyNewPort()
                    }
                }
            }.ToJson());
        }
        private void TuntapSetup()
        {
            TuntapTransfer tuntapTransfer = LinkerMessengerEntry.GetService<TuntapTransfer>();
            tuntapTransfer.OnSetupBefore += () =>
            {
                try
                {
                    MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BindVpnService) != Permission.Granted)
                        {
                            ActivityCompat.RequestPermissions(Platform.CurrentActivity, new string[] { Manifest.Permission.BindVpnService }, 0);
                        }
                        Intent intent = VpnService.Prepare(this);
                        if (intent != null)
                        {
                            //Android.Widget.Toast.MakeText(this, "请允许Linker使用VPN服务", Android.Widget.ToastLength.Short)?.Show();
                            Platform.CurrentActivity.StartActivityForResult(intent, 0x0F);
                        }
                    }).Wait();
                }
                catch (Exception)
                {
                }
                try
                {
                    vpnIntent = new Intent(Android.App.Application.Context, typeof(VpnServiceLinker));
                    Android.App.Application.Context.StartService(vpnIntent);
                    Task.Delay(3000).Wait();
                }
                catch (Exception)
                {
                }
            };
            tuntapTransfer.OnShutdownBefore += () =>
            {
                if (vpnIntent != null)
                {
                    Android.App.Application.Context.StopService(vpnIntent);
                    vpnIntent = null;
                }
            };
        }

    }

    /// <summary>
    /// VPN设备
    /// </summary>
    public sealed class LinkerVpnDevice : ILinkerTunDevice
    {
        private string name = string.Empty;
        public string Name => name;
        public bool Running => fd != 0;

        private IPAddress address;
        private byte prefixLength = 24;

        private ParcelFileDescriptor vpnInterface;
        int fd = 0;
        VpnService vpnService;
        VpnService.Builder builder;
        FileInputStream vpnInput;
        FileOutputStream vpnOutput;

        LinkerTunDeviceRouteItem[] routes = [];

        public LinkerVpnDevice()
        {
        }

        public void SetVpnService(VpnService vpnService)
        {
            this.vpnService = vpnService;
        }

        public bool Setup(LinkerTunDeviceSetupInfo info, out string error)
        {
            error = string.Empty;
            if (info.Address.Equals(IPAddress.Any)) return false;
            if (vpnService == null) return false;


            this.name = info.Name;
            this.address = info.Address;
            this.prefixLength = info.PrefixLength;

            Build();

            return true;
        }
        private void Build()
        {
            try
            {
                builder = new VpnService.Builder(vpnService);
                builder.SetMtu(1420)
                    .AddAddress(address.ToString(), prefixLength)
                    .AddDnsServer("8.8.8.8").SetBlocking(false);

                foreach (var item in routes)
                {
                    builder.AddRoute(NetworkHelper.ToNetworkIP(item.Address, item.PrefixLength).ToString(), item.PrefixLength);
                }

                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                    builder.SetMetered(false);
                vpnInterface = builder.SetSession(name).Establish();

                vpnInput = new FileInputStream(vpnInterface.FileDescriptor);
                vpnOutput = new FileOutputStream(vpnInterface.FileDescriptor);
                fd = vpnInterface.Fd;
            }
            catch (Exception)
            { }
        }


       private readonly byte[] buffer = new byte[128 * 1024];
        private readonly byte[] bufferWrite = new byte[128 * 1024];
        public byte[] Read(out int length)
        {
            length = 0;
            try
            {
                while (fd > 0 && vpnInput != null)
                {
                    length = vpnInput.Read(buffer, 4, buffer.Length - 4);
                    if (length > 0)
                    {
                        length.ToBytes(buffer.AsSpan());
                        length += 4;

                        return buffer;
                    }
                    WaitForTun();
                }
            }
            catch (Exception ex)
            {
                fd = 0;
                System.Console.WriteLine($"vpn read {ex}");
            }
            return Helper.EmptyArray;
        }

        private readonly object writeLockObj = new object();
        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            if (fd == 0 || vpnOutput == null) return false;
            try
            {
                lock (writeLockObj)
                {
                    buffer.CopyTo(bufferWrite);
                    vpnOutput.Write(bufferWrite, 0, buffer.Length);
                    vpnOutput.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                fd = 0;
                System.Console.WriteLine($"vpn write {ex}");
            }
            return false;
        }

        public void Shutdown()
        {
            builder?.Dispose();
            vpnInterface?.FileDescriptor.Dispose();
            vpnInterface?.Close();
            vpnInterface = null;

            vpnInput?.Dispose();
            vpnInput = null;

            vpnOutput?.Dispose();
            vpnOutput = null;

            fd = 0;
        }
        public void Refresh()
        {
        }

        public void SetMtu(int value)
        {
        }
        public void SetNat(out string error)
        {
            error = string.Empty;
        }
        public void RemoveNat(out string error)
        {
            error = string.Empty;
        }

        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
        }
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
        }

        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            return new List<LinkerTunDeviceForwardItem>();
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips"></param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips)
        {
            LinkerTunDeviceRouteItem[] _routes = ips.Select(c => new LinkerTunDeviceRouteItem { Address = c.Address, PrefixLength = c.PrefixLength }).ToArray();
            bool diff = _routes.Select(c => $"{c.Address}/{c.PrefixLength}").Except(routes.Select(c => $"{c.Address}/{c.PrefixLength}")).Any();
            routes = _routes;

            if (diff)
            {
                Shutdown();
                Build();
            }
        }
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ips)
        {
        }
        public async Task<bool> CheckAvailable(bool order = false)
        {
            return await Task.FromResult(fd > 0);
        }


        private void WaitForTun()
        {
            var pollFd = new PollFD
            {
                fd = fd,
                events = 0x001,
                revents = 0
            };
            LinuxAPI.poll([pollFd], 1, 500);
        }

        public static class LinuxAPI
        {
            [DllImport("libc", SetLastError = true)]
            public static extern int poll([In, Out] PollFD[] fds, int nfds, int timeout);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct PollFD
        {
            public int fd;
            public short events;
            public short revents;
        }
    }

    /// <summary>
    /// 管理页面的文件读取，跟PC的不太一样
    /// </summary>
    public sealed class WebServerFileReader : IWebServerFileReader
    {
        ReceiveDataBuffer receiveDataBuffer = new ReceiveDataBuffer();
        byte[] buffer = new byte[4 * 1024];
        public byte[] Read(string root, string fileName, out DateTime lastModified)
        {
            lastModified = DateTime.Now;
            fileName = System.IO.Path.Join("public/web", fileName);
            using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync(fileName).Result;
            using MemoryStream memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            return memoryStream.ToArray();

        }
    }

    /// <summary>
    /// 获取系统信息
    /// </summary>
    public sealed class SystemInformation : ITuntapSystemInformation
    {
        public string Get()
        {
            var deviceInfo = DeviceInfo.Current;
            return $"{deviceInfo.Manufacturer} {deviceInfo.Name} {deviceInfo.VersionString} {deviceInfo.Platform} {deviceInfo.Idiom.ToString()}";
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    public sealed class UpdaterInstaller : messenger.updater.UpdaterInstaller
    {
        private readonly IUpdaterCommonStore updaterCommonTransfer;
        public UpdaterInstaller(IUpdaterCommonStore updaterCommonTransfer) : base(updaterCommonTransfer)
        {
            this.updaterCommonTransfer = updaterCommonTransfer;
        }
        public override (string, string) DownloadUrlAndSavePath(string version)
        {
            return ($"{updaterCommonTransfer.UpdateUrl}/{version}/linker.apk", System.IO.Path.Join(FileSystem.Current.AppDataDirectory, "linker.apk"));
        }

        public override async Task Install(Action<long, long> processs)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await RequestRequiredPermissions(Platform.CurrentActivity);
                await InstallApk(Platform.CurrentActivity);
                processs(100, 100);
            });
        }

        public override void Clear()
        {
        }

        private async Task InstallApk(Context context)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var file = new Java.IO.File(System.IO.Path.Join(FileSystem.Current.AppDataDirectory, "linker.apk"));

                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    // Android 7.0及以上版本需要使用FileProvider
                    var apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(context,
                        $"{context.ApplicationContext.PackageName}.fileprovider", file);

                    var installIntent = new Intent(Intent.ActionInstallPackage);
                    installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
                    installIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    installIntent.AddFlags(ActivityFlags.NewTask);
                    context.StartActivity(installIntent);
                }
                else
                {
                    // 传统安装方式
                    var apkUri = Android.Net.Uri.FromFile(file);
                    var installIntent = new Intent(Intent.ActionView);
                    installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
                    installIntent.SetFlags(ActivityFlags.NewTask);
                    context.StartActivity(installIntent);
                }
            });
        }
        private async Task<bool> RequestRequiredPermissions(Context context)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // 检查并请求存储权限
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    if (status != PermissionStatus.Granted)
                        return false;
                }

                // 检查安装未知应用权限（针对Android 8.0+）
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O && !context.PackageManager.CanRequestPackageInstalls())
                {
                    var intent = new Intent(Android.Provider.Settings.ActionManageUnknownAppSources)
                        .SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
                    context.StartActivity(intent);
                    return false;
                }
                return true;
            });
        }
    }
}