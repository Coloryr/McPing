using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McPing
{
    class Tools
    {
        public static Bitmap ZoomImage(Bitmap bitmap, int destHeight, int destWidth)
        {
            try
            {
                int width = 0, height = 0;
                //按比例缩放             
                int sourWidth = bitmap.Width;
                int sourHeight = bitmap.Height;
                if (sourHeight > destHeight || sourWidth > destWidth)
                {
                    if (sourWidth * destHeight > sourHeight * destWidth)
                    {
                        width = destWidth;
                        height = destWidth * sourHeight / sourWidth;
                    }
                    else
                    {
                        height = destHeight;
                        width = sourWidth * destHeight / sourHeight;
                    }
                }
                else
                {
                    width = sourWidth;
                    height = sourHeight;
                }
                Bitmap destBitmap = new(width, height);
                Graphics g = Graphics.FromImage(destBitmap);
                g.Clear(Color.Transparent);
                //设置画布的描绘质量           
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, new Rectangle(0, 0, width, height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel);
                g.Dispose();
                bitmap.Dispose();
                return destBitmap;
            }
            catch
            {
                return bitmap;
            }
        }
    }
}
