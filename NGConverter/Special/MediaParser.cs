using System;
using System.Linq;

using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.NGConverter.Models;


namespace Gemipedia.NGConverter.Special
{
    /// <summary>
    /// Converts the various image widgets
    /// </summary>
    public static class MediaParser
    {
        static TextExtractor textExtractor;

        public static MediaItem Convert(IElement imageContainer, IElement captionContainer)
            => IsVideo(imageContainer) ?
                ConvertVideo(imageContainer, captionContainer) :
                ConvertImage(imageContainer, captionContainer);
        
        private static MediaItem ConvertImage(IElement imageContainer, IElement captionContainer)
        {
            var url = CommonUtils.GetImageUrl(imageContainer.QuerySelector("img"));
            if (url == null)
            {
                return null;
            }

            textExtractor = new TextExtractor();
            var description = GetDescription(imageContainer, captionContainer);
            return new MediaItem
            {
                ArticleLinks = textExtractor.ArticleLinks,
                Caption = description,
                Url = CommonUtils.MediaProxyUrl(url),
            };
        }

        private static MediaItem ConvertVideo(IElement imageContainer, IElement captionContainer)
        {
            var videoElement = ParseVideo(imageContainer);

            string imageUrl = GetPosterUrl(videoElement);
            string videoUrl = GetVideoUrl(videoElement);
            if(imageUrl == null || videoUrl == null)
            {
                return null;
            }

            textExtractor = new TextExtractor();
            var description = GetDescription(imageContainer, captionContainer);

            return new VideoItem
            {
                ArticleLinks = textExtractor.ArticleLinks,
                Caption = description,
                Url = CommonUtils.MediaProxyUrl(imageUrl),
                VideoUrl = videoUrl,
                VideoDescription = GetVideoDescription(videoElement)
            };
        }

        private static IElement ParseVideo(IElement imageContainer)
            => imageContainer.QuerySelector("video");

        private static bool IsVideo(IElement imageContainer)
            => (imageContainer.QuerySelector("video") != null);

        private static string GetPosterUrl(IElement videoElement)
            => CommonUtils.EnsureHttps(videoElement?.GetAttribute("poster") ?? null);

        private static string GetVideoUrl(IElement videoElement)
            => CommonUtils.EnsureHttps(videoElement?.QuerySelector("source").GetAttribute("src") ?? null);

        private static string GetVideoDescription(IElement videoElement)
            => "🎦 " + (videoElement?.QuerySelector("source").GetAttribute("data-title") ?? "Video File");

       

        private static string GetDescription(IElement imageContainer, IElement captionContainer)
        {
            //first see if there is a caption
            var text = textExtractor.GetText(captionContainer);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
            //fall back to the ALT text
            text = GetImageAlt(imageContainer);
            return (text.Length > 0) ? text : "Article Image";
        }

        private static string GetImageAlt(IElement element)
            => StripImageExtensions(element.QuerySelector("img")?.GetAttribute("alt") ?? "");

        //For some alt text, sometimes the filename is used, so strip off any trailing extension to improve readability
        private static string StripImageExtensions(string alt)
        {
            alt = StripExtension(alt, "jpeg");
            alt = StripExtension(alt, "jpg");
            alt = StripExtension(alt, "png");
            alt = StripExtension(alt, "gif");
            alt = StripExtension(alt, "svg");
            return alt;
        }

        private static string StripExtension(string alt, string ext)
            => (alt.Length > (ext.Length) + 1 &&
                alt.EndsWith($".{ext}")) ? alt.Substring(0, alt.Length - (ext.Length) - 1) : alt;
    }
}
