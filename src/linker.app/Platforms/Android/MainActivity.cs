using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;

namespace linker.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public const int VPN_RESULT_CODE = 0x0F;

        protected override void OnCreate(Bundle savedInstanceState)
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
                StartActivityForResult(intent, VPN_RESULT_CODE);
            }
            else
            {
                OnActivityResult(VPN_RESULT_CODE, Result.Ok, null);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == VPN_RESULT_CODE && resultCode == Result.Ok)
            {
                StartService(new Intent(this, typeof(VpnServiceLinker)));
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
    }


    [Service(Label = "VpnServiceLinker", Name = "com.snltty.linker.app.VpnServiceLinker", Enabled = true, Permission = "android.permission.BIND_VPN_SERVICE")]
    public class VpnServiceLinker : VpnService
    {
        private static readonly string TAG = "VpnServiceLinker";
        private CancellationTokenSource cts = new CancellationTokenSource();
        private ParcelFileDescriptor vpnInterface;

        private string ip = "10.18.18.2";
        private int prefixLength = 24;

        private string dns = "8.8.8.8";

        public override void OnCreate()
        {
            base.OnCreate();
            ConfigureVpn();
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }
        public override void OnDestroy()
        {
            cts.Cancel();
            base.OnDestroy();
        }

        private void ConfigureVpn()
        {
            try
            {
                Builder builder = new Builder(this);
                builder.SetMtu(1420).AddAddress(ip, prefixLength).AddDnsServer(dns);
                vpnInterface = builder.SetSession(TAG).Establish();
                if (vpnInterface != null)
                {
                    Android.Util.Log.Error(TAG, "==============================================VPN is connected");
                    Read();
                }
                else
                {
                    Android.Util.Log.Error(TAG, "==============================================Failed to establish VPN");
                }
            }
            catch (System.Exception ex)
            {
                Android.Util.Log.Error(TAG, $"==============================================VPN Exception{ex}");
            }
        }
        private void Read()
        {
            Task.Run(async () =>
            {
                FileChannel vpnInput = new FileInputStream(vpnInterface.FileDescriptor).Channel;
                FileChannel vpnOutput = new FileOutputStream(vpnInterface.FileDescriptor).Channel;

                ByteBuffer bufferToNetwork = ByteBuffer.Allocate(2048);
                byte[] buffer = new byte[2048];

                while (cts.IsCancellationRequested == false)
                {
                    int length = await vpnInput.ReadAsync(bufferToNetwork).ConfigureAwait(false);

                    if (length > 0)
                    {
                        bufferToNetwork.Flip();
                        bufferToNetwork.Get(buffer, 0, length);
                        bufferToNetwork.Clear();

                        Android.Util.Log.Error("VPN READ", $">>>>>>>>>>>>>>>>>>>>>>{length}");
                    }
                    else
                    {
                        await Task.Delay(0).ConfigureAwait(false);
                    }
                }
            });
        }
    }
}
