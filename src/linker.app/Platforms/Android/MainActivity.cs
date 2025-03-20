using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Nfc;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.IO;
using Java.Net;
using Java.Nio;
using Java.Nio.Channels;
using linker.libs;
using linker.messenger.entry;
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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Window.SetFlags(WindowManagerFlags.TranslucentStatus, WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

            base.OnCreate(savedInstanceState);

            ConfigureVpn();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == VPN_RESULT_CODE && resultCode == Result.Ok)
            {
                //StartService(new Intent(this, typeof(VpnServiceLinker)));
            }
            /*
            intent = new Intent(Android.App.Application.Context,typeof(VpnServiceLinker));
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                Android.App.Application.Context.StartForegroundService(intent);
            }
            else
            {
                Android.App.Application.Context.StartService(intent);
            }
            */
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
        private void RunLinker()
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("snltty.pfx").Result;
            using var fileStream = System.IO.File.Create(System.IO.Path.Join(FileSystem.Current.AppDataDirectory, "snltty.pfx"));
            stream.CopyTo(fileStream);
            stream.Close();
            fileStream.Close();

            LoggerHelper.Instance.OnLogger += (model) =>
            {
                string line = $"[{model.Type,-7}][{model.Time:yyyy-MM-dd HH:mm:ss}]:{model.Content}";
                Android.Util.Log.Debug("linker", line);
            };
            Helper.currentDirectory = FileSystem.Current.AppDataDirectory;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic["Client"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                CApi = new { ApiPort = 0, WebPort = 0 },
                Servers = new object[] { new { Name = "linker", Host = "linker.snltty.com:1802", UserId = Guid.NewGuid().ToString() } },
                Groups = new object[] { new { Name = "Linker", Id = "Linker", Password = "EFFBF3B7-05F5-DBB3-751E-E68F2849AA08" } }
            }))); ;
            dic["Common"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Modes = new string[] { "client" }, LoggerType = 0 })));
            LinkerMessengerEntry.Initialize();
            LinkerMessengerEntry.Build();
            LinkerMessengerEntry.Setup(ExcludeModule.None, dic);
        }
    }


    [Service(Label = "VpnServiceLinker", Name = "com.snltty.linker.app.VpnServiceLinker", Enabled = true, Permission = "android.permission.BIND_VPN_SERVICE")]
    public class VpnServiceLinker : VpnService, ILinkerTunDeviceCallback, ITuntapProxyCallback
    {
        LinkerVpnDevice linkerVpnDevice;
        TuntapConfigTransfer tuntapConfigTransfer;
        TuntapProxy tuntapProxy;
        TuntapDecenter  tuntapDecenter;
        public VpnServiceLinker()
        {
            LinkerTunDeviceAdapter adapter = LinkerMessengerEntry.GetService<LinkerTunDeviceAdapter>();

            linkerVpnDevice = new LinkerVpnDevice(this);
            tuntapConfigTransfer = LinkerMessengerEntry.GetService<TuntapConfigTransfer>();
            tuntapProxy = LinkerMessengerEntry.GetService<TuntapProxy>();
            tuntapProxy.Callback = this;
            tuntapDecenter = LinkerMessengerEntry.GetService<TuntapDecenter>();
           
            adapter.Initialize(linkerVpnDevice,this);
        }
        public override void OnCreate()
        {
            base.OnCreate();

            string name = string.IsNullOrWhiteSpace(tuntapConfigTransfer.Info.Name) ? "linker" : tuntapConfigTransfer.Info.Name;
            linkerVpnDevice.Setup(name, tuntapConfigTransfer.Info.IP, tuntapConfigTransfer.Info.IP, tuntapConfigTransfer.Info.PrefixLength, out string error);
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            linkerVpnDevice.Shutdown();
        }

        public async Task Callback(LinkerTunDevicPacket packet)
        {
            if (packet.IPV4Broadcast || packet.IPV6Multicast)
            {
                if ((tuntapConfigTransfer.Switch & TuntapSwitch.Multicast) == TuntapSwitch.Multicast)
                {
                    return;
                }
            }
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }
        public async ValueTask Close(ITunnelConnection connection)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
        public void Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer)
        {
            linkerVpnDevice.Write(buffer);
        }
        public async ValueTask NotFound(uint ip)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
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
        Intent intent;
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
            this.name = name;
            this.address = address;
            this.prefixLength = prefixLength;

            builder = new VpnService.Builder(vpnService);
            builder.SetMtu(1420).AddAddress(address.ToString(), prefixLength).AddDnsServer("8.8.8.8").SetBlocking(false);
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
                    length = vpnInput.Read(buffer);
                    if (length > 0)
                    {
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
                vpnOutput.Write(bufferWrite,0, buffer.Length);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        public void Shutdown()
        {
            builder.Dispose();
            fd = 0;
            vpnInterface = null;
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
            /*
            foreach (var item in ips)
            {
                builder.AddRoute(item.Address.ToString(), item.PrefixLength);
            }
           */
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ips)
        {
            /*
            foreach (var item in ips)
            {
                builder.ExcludeRoute(new IpPrefix(InetAddress.GetByName(item.Address.ToString()), item.PrefixLength));
            }
            */
        }
        public async Task<bool> CheckAvailable()
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
    }
}
