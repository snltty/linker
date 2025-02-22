using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Views;
using Java.IO;

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

            Intent intent = new Intent(this, typeof(VpnServiceLinker));
            StartForegroundService(intent);
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

    [Service]
    public class VpnServiceLinker : VpnService
    {
        private static readonly string TAG = "VpnServiceLinker";

        public override void OnCreate()
        {
            base.OnCreate();
          
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            ConfigureVpn();
            return StartCommandResult.Sticky;
        }
        private void ConfigureVpn()
        {
            var builder = new Builder(this);
            builder.SetSession(TAG)
                   .SetMtu(1420)
                   .AddAddress("10.0.0.2", 24)
                   .AddRoute("0.0.0.0", 0);
            var vpnInterface = builder.Establish();
            if (vpnInterface != null)
            {
                Android.Util.Log.Debug(TAG, "VPN is connected");
            }
            else
            {
                Android.Util.Log.Error(TAG, "Failed to establish VPN");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            // 停止 VPN 连接
            Android.Util.Log.Debug(TAG, "VPN connection is stopped");
        }
    }
}
