using System;
namespace cmonitor.server.client.reports.screen.aforge
{
   
    [Serializable]
    public struct Point
    {
        public float X;

        public float Y;

        public Point( float x, float y )
        {
            this.X = x;
            this.Y = y;
        }

        public float DistanceTo( Point anotherPoint )
        {
            float dx = X - anotherPoint.X;
            float dy = Y - anotherPoint.Y;

            return (float) System.Math.Sqrt( dx * dx + dy * dy );
        }

        public float SquaredDistanceTo( Point anotherPoint )
        {
            float dx = X - anotherPoint.X;
            float dy = Y - anotherPoint.Y;

            return dx * dx + dy * dy;
        }

        public static Point operator +( Point point1, Point point2 )
        {
            return new Point( point1.X + point2.X, point1.Y + point2.Y );
        }

        public static Point Add( Point point1, Point point2 )
        {
            return new Point( point1.X + point2.X, point1.Y + point2.Y );
        }

        public static Point operator -( Point point1, Point point2 )
        {
            return new Point( point1.X - point2.X, point1.Y - point2.Y );
        }

        public static Point Subtract( Point point1, Point point2 )
        {
            return new Point( point1.X - point2.X, point1.Y - point2.Y );
        }

        public static Point operator +( Point point, float valueToAdd )
        {
            return new Point( point.X + valueToAdd, point.Y + valueToAdd );
        }

        public static Point Add( Point point, float valueToAdd )
        {
            return new Point( point.X + valueToAdd, point.Y + valueToAdd );
        }

        public static Point operator -( Point point, float valueToSubtract )
        {
            return new Point( point.X - valueToSubtract, point.Y - valueToSubtract );
        }

        public static Point Subtract( Point point, float valueToSubtract )
        {
            return new Point( point.X - valueToSubtract, point.Y - valueToSubtract );
        }

        public static Point operator *( Point point, float factor )
        {
            return new Point( point.X * factor, point.Y * factor );
        }

        public static Point Multiply( Point point, float factor )
        {
            return new Point( point.X * factor, point.Y * factor );
        }

        public static Point operator /( Point point, float factor )
        {
            return new Point( point.X / factor, point.Y / factor );
        }

        public static Point Divide( Point point, float factor )
        {
            return new Point( point.X / factor, point.Y / factor );
        }

        public static bool operator ==( Point point1, Point point2 )
        {
            return ( ( point1.X == point2.X ) && ( point1.Y == point2.Y ) );
        }

        public static bool operator !=( Point point1, Point point2 )
        {
            return ( ( point1.X != point2.X ) || ( point1.Y != point2.Y ) );
        }

        public override bool Equals( object obj )
        {
            return ( obj is Point ) ? ( this == (Point) obj ) : false;
        }

        public override int GetHashCode( )
        {
            return X.GetHashCode( ) + Y.GetHashCode( );
        }

        public static explicit operator IntPoint( Point point )
        {
            return new IntPoint( (int) point.X, (int) point.Y );
        }

        public static implicit operator DoublePoint( Point point )
        {
            return new DoublePoint( point.X, point.Y );
        } 

        public IntPoint Round( )
        {
            return new IntPoint( (int) Math.Round( X ), (int) Math.Round( Y ) );
        }

        public override string ToString( )
        {
            return string.Format( System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}", X, Y );
        }

        public float EuclideanNorm( )
        {
            return (float) System.Math.Sqrt( X * X + Y * Y );
        }
    }
}
