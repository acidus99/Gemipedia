using System;

using ImageMagick;

namespace Gemipedia
{
    public static class SvgConverter
    {
        public static byte[] ConvertToPng(byte[] svg)
        {
            using (var image = new MagickImage(svg))
            {
                image.Format = MagickFormat.Png;
                return image.ToByteArray();
            }
        }
    }
}
