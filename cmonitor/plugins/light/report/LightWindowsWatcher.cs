using System.Management;
namespace cmonitor.plugins.light.report
{
    public class LightWindowsWatcher : IDisposable
    {
        public event EventHandler<BrightnessChangedEventArgs> BrightnessChanged;

        public class BrightnessChangedEventArgs : EventArgs
        {
            public object newBrightness { get; set; }

            public BrightnessChangedEventArgs(object b)
            {
                newBrightness = b;
            }
        }

        private void WmiEventHandler(object sender, EventArrivedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                if (BrightnessChanged != null)
                {
                    BrightnessChanged(this, new BrightnessChangedEventArgs(e.NewEvent.Properties["Brightness"].Value));
                }
            }
        }

        private readonly ManagementEventWatcher _watcher;

        public LightWindowsWatcher()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var scope = @"root\wmi";
                    var query = "SELECT * FROM WmiMonitorBrightnessEvent";
                    _watcher = new ManagementEventWatcher(scope, query);
                    _watcher.EventArrived += new EventArrivedEventHandler(WmiEventHandler);
                    _watcher.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception {0} Trace {1}", e.Message, e.StackTrace);
                }
            }
        }

        public void Dispose()
        {
            if (OperatingSystem.IsWindows())
            {
                if (_watcher != null)
                {
                    _watcher.Stop();
                }

                _watcher.Dispose();
            }
        }
    }
}
