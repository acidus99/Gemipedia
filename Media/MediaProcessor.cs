using System;

using ImageMagick;

namespace Gemipedia.Media
{
    /// <summary>
    /// Reformats media from Wikipedia to better suit Gemini clients
    /// </summary>
    public static class MediaProcessor
    {
        public static MediaContent ProcessImage(byte[] data)
        {
            using (var image = new MagickImage(data))
            {
             
                if(image.Format == MagickFormat.Svg)
                {
                    //convert it to PNG
                    image.Format = MagickFormat.Png;
                    return ToContent(image);
                }
                else if(!image.IsOpaque)
                {
                    //add a white background to transparent images to
                    //make them visible on clients with a dark theme
                    image.BackgroundColor = new MagickColor("white");
                    image.Alpha(AlphaOption.Remove);
                    return ToContent(image);
                }
                else
                {
                    //nothing needed (e.g. JPG, etc) so pass it through
                    return new MediaContent
                    {
                        Data = data,
                        MimeType = image.FormatInfo.MimeType
                    };
                }
            }
        }

        private static MediaContent ToContent(MagickImage image)
            => new MediaContent
            {
                Data = image.ToByteArray(),
                MimeType = image.FormatInfo.MimeType
            };
    }
}
