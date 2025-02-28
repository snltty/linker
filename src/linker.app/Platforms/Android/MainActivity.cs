using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Health.Connect.DataTypes.Units;
using Android.Net;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.IO;
using System.Net.NetworkInformation;

namespace linker.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Window.SetFlags(WindowManagerFlags.TranslucentStatus, WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

            base.OnCreate(savedInstanceState);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BindVpnService) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.BindVpnService }, 0);
            }

            var intent = VpnService.Prepare(this);
            if (intent != null)
            {
                StartActivityForResult(intent, 0);
            }
            StartForegroundService(new Intent(this, typeof(VpnServiceLinker)));
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

    [Service(Name = "com.snltty.linker.app.VpnServiceLinker", Permission = "android.permission.BIND_VPN_SERVICE")]
    public class VpnServiceLinker : VpnService
    {
        private static readonly string TAG = "VpnServiceLinker";

        public override void OnCreate()
        {
            base.OnCreate();
            ConfigureVpn();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {

            return StartCommandResult.Sticky;
        }
        private void ConfigureVpn()
        {
            try
            {
                Builder builder = new Builder(this);
                builder.SetMtu(1420)
                       .AddAddress("10.18.18.2", 24)
                       .AddRoute("0.0.0.0", 0)
                       .AddDnsServer("8.8.8.8");
                var vpnInterface = builder.SetSession(TAG).Establish();
                if (vpnInterface != null)
                {
                    Android.Util.Log.Error(TAG, "==============================================VPN is connected");
                    /*
                    Task.Run(async () =>
                    {
                        try
                        {
                            FileInputStream fs = new FileInputStream(vpnInterface.FileDescriptor);
                            byte[] bytes = new byte[2*1024];

                            while (true)
                            {
                                int length = await fs.ReadAsync(bytes);
                                Android.Util.Log.Error(TAG, $"=============================================VPN read:{length}");
                            }
                        }
                        catch (Exception ex)
                        {

                            Android.Util.Log.Error(TAG, $"=============================================VPN read:{ex}");
                        }
                    });
                    */
                    /*
                    Task.Run(async () =>
                    {
                        try
                        {
                            using Ping ping = new Ping();
                            ping.Send("10.18.18.2");

                            await Task.Delay(1000);
                        }
                        catch (Exception ex)
                        {

                            Android.Util.Log.Error(TAG, $"=============================================VPN read:{ex}");
                        }
                    });
                    */
                }
                else
                {
                    Android.Util.Log.Error(TAG, "==============================================Failed to establish VPN");
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(TAG, $"==============================================VPN Exception{ex}");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
