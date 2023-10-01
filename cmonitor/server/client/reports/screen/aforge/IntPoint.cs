using System.Drawing;
namespace cmonitor.server.client.reports.screen.aforge
{


    [Serializable]
    public struct IntPoint
    {
        public int X;

        public int Y;

        public IntPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }


        public static implicit operator Point(IntPoint point)
        {
            return new Point(point.X, point.Y);
        }

        public static implicit operator DoublePoint(IntPoint point)
        {
            return new DoublePoint(point.X, point.Y);
        }
    }
}
