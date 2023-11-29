using MemoryPack;
using System.Text.Json.Serialization;

namespace cmonitor.server.client.reports.screen
{
    public class DesktopFrame
    {
        public Memory<byte> FullImage { get; internal set; }
        public Memory<byte> RegionImage { get; internal set; }

        public Rectangle[] UpdatedRegions { get; internal set; }
        public MovedRegion[] MovedRegions { get; internal set; }

    }

    public struct MovedRegion
    {
        public System.Drawing.Point Source { get; internal set; }

        public Rectangle Destination { get; internal set; }
    }

    public struct Scale
    {
        public Scale(int x, int y, float scale)
        {
            X = x;
            Y = y;
            Value = scale;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public float Value { get; set; }
    }


    [MemoryPackable]
    public partial struct Rect
    {
        public Rect(int width, int height)
        {
            Width = width;
            Height = height;
        }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    [MemoryPackable]
    public partial struct Rectangle
    {
        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Remove = false;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        [MemoryPackIgnore, JsonIgnore]
        public bool Remove { get; set; }

        public readonly bool IntersectsWith(Rectangle rect) => IntersectsWithX(rect) && IntersectsWithY(rect);
        public readonly bool IntersectsWithX(Rectangle rect) => (rect.X < X + Width) && (X < rect.X + rect.Width);
        public readonly bool IntersectsWithY(Rectangle rect) => (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);

        public readonly bool NearWith(Rectangle rect, int dist) => (NearWithX(rect, dist) && IntersectsWithY(rect)) || (NearWithY(rect, dist) && IntersectsWithX(rect));
        public readonly bool NearWithX(Rectangle rect, int dist) => Math.Min(Math.Abs(X + Width - rect.X), Math.Abs(rect.X + rect.Width - X)) <= dist;
        public readonly bool NearWithY(Rectangle rect, int dist) => Math.Min(Math.Abs(Y + Height - rect.Y), Math.Abs(rect.Y + rect.Height - Y)) <= dist;


        public readonly Rectangle Union(Rectangle b)
        {
            Rectangle a = this;
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public static Rectangle[] UnionRectangles(Rectangle[] rects)
        {
            int removeLength = 0;
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].Remove) continue;
                for (int j = i + 1; j < rects.Length; j++)
                {
                    if (rects[j].Remove) continue;
                    if (rects[i].IntersectsWith(rects[j]) || rects[i].NearWith(rects[j], 5))
                    {
                        rects[i] = rects[i].Union(rects[j]);
                        rects[j].Remove = true;
                        removeLength++;
                    }
                }
            }
            if (removeLength == 0) return rects;

            Rectangle[] result = new Rectangle[rects.Length - removeLength];
            for (int i = 0, index = 0; i < rects.Length; i++)
            {
                if (rects[i].Remove == false)
                {
                    result[index] = rects[i];
                    index++;
                }
            }

            return result;
        }
    }


}
