using System.Drawing;

namespace cmonitor.server.client.reports.screen.sharpDX
{
    public struct MovedRegion
    {
        public System.Drawing.Point Source { get; internal set; }

        public System.Drawing.Rectangle Destination { get; internal set; }
    }
}
