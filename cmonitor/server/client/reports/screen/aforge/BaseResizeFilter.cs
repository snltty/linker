using System.Drawing;

namespace cmonitor.server.client.reports.screen.aforge
{
    public abstract class BaseResizeFilter : BaseTransformationFilter
    {
        protected int newWidth;

        protected int newHeight;

        public int NewWidth
        {
            get { return newWidth; }
            set { newWidth = Math.Max( 1, value ); }
        }

        public int NewHeight
        {
            get { return newHeight; }
            set { newHeight = Math.Max( 1, value ); }
        }

        protected BaseResizeFilter( int newWidth, int newHeight )
        {
            this.newWidth  = newWidth;
            this.newHeight = newHeight;
        }

        protected override Size CalculateNewImageSize()
        {
            return new Size( newWidth, newHeight );
        }
    }
}
