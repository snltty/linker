using System;

namespace cmonitor.server.client.reports.screen.aforge
{
    

    [Serializable]
    public struct DoublePoint
    {
        public double X;

        public double Y;

        public DoublePoint( double x, double y )
        {
            this.X = x;
            this.Y = y;
        }

        public static bool operator ==( DoublePoint point1, DoublePoint point2 )
        {
            return ( ( point1.X == point2.X ) && ( point1.Y == point2.Y ) );
        }

        public static bool operator !=( DoublePoint point1, DoublePoint point2 )
        {
            return ( ( point1.X != point2.X ) || ( point1.Y != point2.Y ) );
        }

        public override bool Equals( object obj )
        {
            return ( obj is DoublePoint ) ? ( this == (DoublePoint) obj ) : false;
        }

        public override int GetHashCode( )
        {
            return X.GetHashCode( ) + Y.GetHashCode( );
        }

        public static explicit operator IntPoint( DoublePoint point )
        {
            return new IntPoint( (int) point.X, (int) point.Y );
        }

        public static explicit operator Point( DoublePoint point )
        {
            return new Point( (float) point.X, (float) point.Y );
        }
    }
}
