using System;
using System.Drawing;

namespace cmonitor.server.client.reports.screen.aforge
{
    public class RGB
    {
        public const short R = 2;

        public const short G = 1;

        public const short B = 0;

        public const short A = 3;

        public byte Red;

        public byte Green;

        public byte Blue;

        public byte Alpha;

        public System.Drawing.Color Color
        {
            get { return Color.FromArgb( Alpha, Red, Green, Blue ); }
            set
            {
                Red   = value.R;
                Green = value.G;
                Blue  = value.B;
                Alpha = value.A;
            }
        }

        public RGB( )
        {
            Red   = 0;
            Green = 0;
            Blue  = 0;
            Alpha = 255;
        }

        public RGB( byte red, byte green, byte blue )
        {
            this.Red   = red;
            this.Green = green;
            this.Blue  = blue;
            this.Alpha = 255;
        }

        public RGB( byte red, byte green, byte blue, byte alpha )
        {
            this.Red   = red;
            this.Green = green;
            this.Blue  = blue;
            this.Alpha = alpha;
        }

        public RGB( System.Drawing.Color color )
        {
            this.Red   = color.R;
            this.Green = color.G;
            this.Blue  = color.B;
            this.Alpha = color.A;
        }
    }

    public class HSL
    {
        public int Hue;

        public float Saturation;

        public float Luminance;

        public HSL( ) { }

        public HSL( int hue, float saturation, float luminance )
        {
            this.Hue        = hue;
            this.Saturation = saturation;
            this.Luminance  = luminance;
        }

        public static void FromRGB( RGB rgb, HSL hsl )
        {
            float r = ( rgb.Red   / 255.0f );
            float g = ( rgb.Green / 255.0f );
            float b = ( rgb.Blue  / 255.0f );

            float min = Math.Min( Math.Min( r, g ), b );
            float max = Math.Max( Math.Max( r, g ), b );
            float delta = max - min;

            // get luminance value
            hsl.Luminance = ( max + min ) / 2;

            if ( delta == 0 )
            {
                // gray color
                hsl.Hue = 0;
                hsl.Saturation = 0.0f;
            }
            else
            {
                // get saturation value
                hsl.Saturation = ( hsl.Luminance <= 0.5 ) ? ( delta / ( max + min ) ) : ( delta / ( 2 - max - min ) );

                // get hue value
                float hue;

                if ( r == max )
                {
                    hue = ( ( g - b ) / 6 ) / delta;
                }
                else if ( g == max )
                {
                    hue = ( 1.0f / 3 ) + ( ( b - r ) / 6 ) / delta; 
                }
                else
                {
                    hue = ( 2.0f / 3 ) + ( ( r - g ) / 6 ) / delta;
                }

                // correct hue if needed
                if ( hue < 0 )
                    hue += 1;
                if ( hue > 1 )
                    hue -= 1;

                hsl.Hue = (int) ( hue * 360 );
            }
        }

        public static HSL FromRGB( RGB rgb )
        {
            HSL hsl = new HSL( );
            FromRGB( rgb, hsl );
            return hsl;
        }

        public static void ToRGB( HSL hsl, RGB rgb )
        {
            if ( hsl.Saturation == 0 )
            {
                // gray values
                rgb.Red = rgb.Green = rgb.Blue = (byte) ( hsl.Luminance * 255 );
            }
            else
            {
                float v1, v2;
                float hue = (float) hsl.Hue / 360;

                v2 = ( hsl.Luminance < 0.5 ) ?
                    ( hsl.Luminance * ( 1 + hsl.Saturation ) ) :
                    ( ( hsl.Luminance + hsl.Saturation ) - ( hsl.Luminance * hsl.Saturation ) );
                v1 = 2 * hsl.Luminance - v2;

                rgb.Red   = (byte) ( 255 * Hue_2_RGB( v1, v2, hue + ( 1.0f / 3 ) ) );
                rgb.Green = (byte) ( 255 * Hue_2_RGB( v1, v2, hue ) );
                rgb.Blue  = (byte) ( 255 * Hue_2_RGB( v1, v2, hue - ( 1.0f / 3 ) ) );
            }
            rgb.Alpha = 255;
        }

        public RGB ToRGB( )
        {
            RGB rgb = new RGB( );
            ToRGB( this, rgb );
            return rgb;
        }

        #region Private members
        // HSL to RGB helper routine
        private static float Hue_2_RGB( float v1, float v2, float vH )
        {
            if ( vH < 0 )
                vH += 1;
            if ( vH > 1 )
                vH -= 1;
            if ( ( 6 * vH ) < 1 )
                return ( v1 + ( v2 - v1 ) * 6 * vH );
            if ( ( 2 * vH ) < 1 )
                return v2;
            if ( ( 3 * vH ) < 2 )
                return ( v1 + ( v2 - v1 ) * ( ( 2.0f / 3 ) - vH ) * 6 );
            return v1;
        }
        #endregion
    }

    public class YCbCr
    {
        public const short YIndex = 0;

        public const short CbIndex = 1;

        public const short CrIndex = 2;

        public float Y;

        public float Cb;

        public float Cr;

        public YCbCr( ) { }

        public YCbCr( float y, float cb, float cr )
        {
            this.Y  = Math.Max(  0.0f, Math.Min( 1.0f, y ) );
            this.Cb = Math.Max( -0.5f, Math.Min( 0.5f, cb ) );
            this.Cr = Math.Max( -0.5f, Math.Min( 0.5f, cr ) );
        }

        public static void FromRGB( RGB rgb, YCbCr ycbcr )
        {
            float r = (float) rgb.Red / 255;
            float g = (float) rgb.Green / 255;
            float b = (float) rgb.Blue / 255;

            ycbcr.Y =  (float) (  0.2989 * r + 0.5866 * g + 0.1145 * b );
            ycbcr.Cb = (float) ( -0.1687 * r - 0.3313 * g + 0.5000 * b );
            ycbcr.Cr = (float) (  0.5000 * r - 0.4184 * g - 0.0816 * b );
        }
        public static YCbCr FromRGB( RGB rgb )
        {
            YCbCr ycbcr = new YCbCr( );
            FromRGB( rgb, ycbcr );
            return ycbcr;
        }

        public static void ToRGB( YCbCr ycbcr, RGB rgb )
        {
            // don't warry about zeros. compiler will remove them
            float r = Math.Max( 0.0f, Math.Min( 1.0f, (float) ( ycbcr.Y + 0.0000 * ycbcr.Cb + 1.4022 * ycbcr.Cr ) ) );
            float g = Math.Max( 0.0f, Math.Min( 1.0f, (float) ( ycbcr.Y - 0.3456 * ycbcr.Cb - 0.7145 * ycbcr.Cr ) ) );
            float b = Math.Max( 0.0f, Math.Min( 1.0f, (float) ( ycbcr.Y + 1.7710 * ycbcr.Cb + 0.0000 * ycbcr.Cr ) ) );

            rgb.Red   = (byte) ( r * 255 );
            rgb.Green = (byte) ( g * 255 );
            rgb.Blue  = (byte) ( b * 255 );
            rgb.Alpha = 255;
        }

        public RGB ToRGB( )
        {
            RGB rgb = new RGB( );
            ToRGB( this, rgb );
            return rgb;
        }
    }
}
