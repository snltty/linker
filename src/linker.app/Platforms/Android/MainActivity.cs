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
using linker.messenger.store.file;
using linker.messenger.tuntap;
using linker.tun;
using linker.tunnel.connection;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace linker.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public const int VPN_RESULT_CODE = 0x0F;
        Intent intent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetLightStatusBar();

            base.OnCreate(savedInstanceState);
            ConfigureVpn();
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
            if (requestCode == VPN_RESULT_CODE && resultCode == Result.Ok)
            {
                intent = new Intent(this, typeof(ForegroundService));
                StartForegroundService(intent);
            }
        }
        protected override void OnStart()
        {
            base.OnStart();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        private void ConfigureVpn()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BindVpnService) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.BindVpnService }, 0);
            }
            var intent = VpnService.Prepare(this);
            if (intent != null)
            {
                StartActivityForResult(intent, VPN_RESULT_CODE);
            }
            else
            {
                OnActivityResult(VPN_RESULT_CODE, Result.Ok, null);
            }
        }

    }


    [Service(Label = "VpnServiceLinker", Name = "com.snltty.linker.app.VpnServiceLinker", Enabled = true, Permission = "android.permission.BIND_VPN_SERVICE")]
    public class VpnServiceLinker : VpnService, ILinkerTunDeviceCallback, ITuntapProxyCallback
    {
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

            tuntapTransfer.Init(new LinkerVpnDevice(this), this);
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
            tuntapTransfer.Shutdown(false);
        }

        public async Task Callback(LinkerTunDevicPacket packet)
        {
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }
        public async ValueTask Close(ITunnelConnection connection)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
        public void Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer)
        {
            tuntapTransfer.Write(buffer);
        }

    }


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

            NotificationChannel notificationChannel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High);
            notificationChannel.EnableLights(true);
            notificationChannel.SetShowBadge(true);
            notificationChannel.LockscreenVisibility = NotificationVisibility.Public;
            NotificationManager manager = (NotificationManager)GetSystemService(NotificationService);
            manager.CreateNotificationChannel(notificationChannel);

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
            Helper.currentDirectory = FileSystem.Current.AppDataDirectory;
            Dictionary<string, string> config = InitConfig();

            InitLogger();

            LinkerMessengerEntry.Initialize();
            LinkerMessengerEntry.AddService<IWebServerFileReader, WebServerFileReader>();
            LinkerMessengerEntry.Build();
            LinkerMessengerEntry.Setup(ExcludeModule.Logger, config);
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
        private Dictionary<string, string> InitConfig()
        {
            try
            {
                System.IO.File.Delete(System.IO.Path.Combine(Helper.currentDirectory, "./configs/", "client.json"));
                System.IO.File.Delete(System.IO.Path.Combine(Helper.currentDirectory, "./configs/", "server.json"));
                System.IO.File.Delete(System.IO.Path.Combine(Helper.currentDirectory, "./configs/", "common.json"));
                System.IO.File.Delete(System.IO.Path.Combine(Helper.currentDirectory, "./configs/", "db.db"));
                System.IO.File.Delete(System.IO.Path.Combine(Helper.currentDirectory, "./configs/", "db-log.db"));
            }
            catch (Exception)
            {
            }
            ConfigClientInfo client = new ConfigClientInfo
            {
            };
            ConfigCommonInfo common = new ConfigCommonInfo
            {
                Modes = new string[] { "client" },
                LoggerType = 0
            };
            return new Dictionary<string, string> {
                {"Client",Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(client)))},
                {"Common", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(common)))}
            };
        }
        private void TuntapSetup()
        {
            TuntapTransfer tuntapTransfer = LinkerMessengerEntry.GetService<TuntapTransfer>();
            tuntapTransfer.OnSetupBefore += () =>
            {
                vpnIntent = new Intent(Android.App.Application.Context, typeof(VpnServiceLinker));
                Android.App.Application.Context.StartService(vpnIntent);
                Task.Delay(3000).Wait();
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

        public LinkerVpnDevice(VpnService vpnService)
        {
            this.vpnService = vpnService;
        }

        public bool Setup(string name, IPAddress address, IPAddress gateway, byte prefixLength, out string error)
        {
            error = string.Empty;
            if (address.Equals(IPAddress.Any)) return false;


            this.name = name;
            this.address = address;
            this.prefixLength = prefixLength;

            builder = new VpnService.Builder(vpnService);
            builder.SetMtu(1420)
                .AddAddress(address.ToString(), prefixLength)
                .AddDnsServer("8.8.8.8").SetBlocking(false);
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                builder.SetMetered(false);
            vpnInterface = builder.SetSession(name).Establish();

            vpnInput = new FileInputStream(vpnInterface.FileDescriptor);
            vpnOutput = new FileOutputStream(vpnInterface.FileDescriptor);
            fd = vpnInterface.Fd;

            return true;
        }

        byte[] buffer = new byte[8 * 1024];
        byte[] bufferWrite = new byte[8 * 1024];
        public byte[] Read(out int length)
        {
            length = 0;
            try
            {
                while (fd > 0)
                {
                    length = vpnInput.Read(buffer, 4, buffer.Length - 4);
                    if (length > 0)
                    {
                        length.ToBytes(buffer);
                        length += 4;
                        return buffer;
                    }
                    WaitForTunRead();
                }
            }
            catch (Exception)
            {
            }
            return buffer;
        }
        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                buffer.CopyTo(bufferWrite);
                vpnOutput.Write(bufferWrite, 0, buffer.Length);
                vpnOutput.Flush();
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        public void Shutdown()
        {
            builder?.Dispose();
            vpnInterface?.FileDescriptor.Dispose();
            vpnInterface?.Close();
            vpnInterface = null;

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

        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ips)
        {
        }
        public async Task<bool> CheckAvailable(bool order = false)
        {
            return await Task.FromResult(fd > 0);
        }


        private void WaitForTunRead()
        {
            WaitForTun(PollEvent.In);
        }
        private void WaitForTunWrite()
        {
            WaitForTun(PollEvent.Out);
        }
        private void WaitForTun(PollEvent pollEvent)
        {
            var pollFd = new PollFD
            {
                fd = fd,
                events = (short)pollEvent
            };

            while (true)
            {
                var result = LinuxAPI.poll([pollFd], 1, -1);
                if (result >= 0)
                    break;
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode == LinuxAPI.EINTR)
                    continue;

                throw new Exception("fail");
            }
        }

        public static class LinuxAPI
        {
            public const int EINTR = 4;
            public const int EAGAIN = 11;

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
        public enum PollEvent : short
        {
            In = 0x001,
            Out = 0x004
        }
    }

    public sealed class WebServerFileReader : IWebServerFileReader
    {
        DateTime lastModified = DateTime.Now;
        public byte[] Read(string root,string fileName, out DateTime lastModified)
        {
            lastModified = this.lastModified;
            fileName = Path.Join("public/web", fileName);
            using Stream fileStream = FileSystem.Current.OpenAppPackageFileAsync(fileName).Result;
            using StreamReader reader = new StreamReader(fileStream);
            return reader.ReadToEnd().ToBytes();
        }
    }

}

