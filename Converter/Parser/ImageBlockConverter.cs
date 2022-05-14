using System;
using System.Linq;

using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Models;


namespace Gemipedia.Converter.Parser
{
    /// <summary>
    /// Converts the various image widgets
    /// </summary>
    public class ImageBlockParser
    {
        TextExtractor textExtractor = new TextExtractor
        {
            ShouldCollapseNewlines = true
        };

        public MediaItem Convert(IElement imageContainer, IElement captionContainer)
            => IsVideo(imageContainer) ?
                ConvertVideo(imageContainer, captionContainer) :
                ConvertImage(imageContainer, captionContainer);
        
        private MediaItem ConvertImage(IElement imageContainer, IElement captionContainer)
        {
            //some image holders can contain <canvas> graphs, charts, etc. So escape if you don't find an img
            var imgTag = imageContainer.QuerySelector("img");
            if(imgTag == null)
            {
                return null;
            }
            var url = CommonUtils.GetImageUrl(imgTag);
            if (url == null)
            {
                return null;
            }

            var description = GetDescription(imageContainer, captionContainer);
            var media = new MediaItem
            {
                Links = GetLinks(),
                Caption = description,
                Url = CommonUtils.MediaProxyUrl(url),
            };
            //if this is an image map, extract those links too
            if (imgTag.HasAttribute("usemap"))
            {
                //look for any maps
                //try and add links from any areas to it
                imageContainer.QuerySelectorAll("map area")
                    .ToList().ForEach(x => media.Links.Add(x));
            }
            return media;
        }

        private MediaItem ConvertVideo(IElement imageContainer, IElement captionContainer)
        {
            var videoElement = ParseVideo(imageContainer);

            string imageUrl = GetPosterUrl(videoElement);
            string videoUrl = GetVideoUrl(videoElement);
            if(imageUrl == null || videoUrl == null)
            {
                return null;
            }

            var description = GetDescription(imageContainer, captionContainer);

            return new VideoItem
            {
                Links = GetLinks(),
                Caption = description,
                Url = CommonUtils.MediaProxyUrl(imageUrl),
                VideoUrl = videoUrl,
                VideoDescription = GetVideoDescription(videoElement)
            };
        }

        private IElement ParseVideo(IElement imageContainer)
            => imageContainer.QuerySelector("video");

        private bool IsVideo(IElement imageContainer)
            => (imageContainer.QuerySelector("video") != null);



        private string GetPosterUrl(IElement videoElement)
            => CommonUtils.EnsureHttps(videoElement?.GetAttribute("poster") ?? null);

        private string GetVideoUrl(IElement videoElement)
            => CommonUtils.EnsureHttps(videoElement?.QuerySelector("source").GetAttribute("src") ?? null);

        private string GetVideoDescription(IElement videoElement)
            => "🎦 " + (videoElement?.QuerySelector("source").GetAttribute("data-title") ?? "Video File");

        private string GetDescription(IElement imageContainer, IElement captionContainer)
        {
            textExtractor.Extract(captionContainer);
            //first see if there is a caption
            var text = textExtractor.Content;
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
            //fall back to the ALT text
            text = GetImageAlt(imageContainer);
            return (text.Length > 0) ? text : "Article Image";
        }

        private ArticleLinkCollection GetLinks()
            => textExtractor.Links;

        private string GetImageAlt(IElement element)
            => StripImageExtensions(element.QuerySelector("img")?.GetAttribute("alt") ?? "");

        //For some alt text, sometimes the filename is used, so strip off any trailing extension to improve readability
        private string StripImageExtensions(string alt)
        {
            alt = StripExtension(alt, "jpeg");
            alt = StripExtension(alt, "jpg");
            alt = StripExtension(alt, "png");
            alt = StripExtension(alt, "gif");
            alt = StripExtension(alt, "svg");
            return alt;
        }

        private string StripExtension(string alt, string ext)
            => (alt.Length > (ext.Length) + 1 &&
                alt.EndsWith($".{ext}")) ? alt.Substring(0, alt.Length - (ext.Length) - 1) : alt;
    }
}
