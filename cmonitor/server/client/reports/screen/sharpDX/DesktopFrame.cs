using System.Drawing;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace cmonitor.server.client.reports.screen.sharpDX
{
    public class DesktopFrame
    {
        public byte[] DesktopImage { get; internal set; }

        public MovedRegion[] MovedRegions { get; internal set; }

        public Rectangle[] UpdatedRegions { get; internal set; }

    }
}
