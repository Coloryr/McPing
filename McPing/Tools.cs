using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace McPing
{
    class Tools
    {
        public static Image ZoomImage(Image bitmap, int destHeight, int destWidth)
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
                bitmap.Mutate((a) =>
                {
                    a.Resize(width, height);
                });
                return bitmap;
            }
            catch
            {
                return bitmap;
            }
        }
    }
}
