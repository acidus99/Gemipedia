using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special
{
    /// <summary>
    /// Converts the various image widgets
    /// </summary>
    public static class MediaParser
    {
        static int montageNumber = 1;
        static int galleryNumber = 1;

        static TextExtractor textExtractor = new TextExtractor
        {
            ShouldCollapseNewlines = true
        };

        public static MediaItem ConvertMedia(IElement imageContainer, IElement captionContainer)
            => IsVideo(imageContainer) ?
                ConvertVideo(imageContainer, captionContainer) :
                ConvertImage(imageContainer, captionContainer);

        public static MediaItem ConvertTimelineInTable(IElement element)
        {
            var timeline = element.QuerySelector("div.timeline-wrapper");
            if (timeline != null)
            {
                //attempt to get a meaningful title for the timeline from the first cell
                textExtractor.Extract(element.QuerySelector("th"), element.QuerySelector("td"));

                return ConvertTimeline(timeline, textExtractor);
            }
            return null;
        }

        public static MediaItem ConvertTimeline(IElement timelineWrapper, ITextContent textContent = null)
        {
            var img = timelineWrapper.QuerySelector("img[usemap]");
            var title = (textContent != null) ? $"Timeline Image: {textContent.Content}" : "Timeline Image";

            if (img != null)
            {
                var media = new MediaItem
                {
                    Url = RouteOptions.MediaProxyUrl(CommonUtils.GetImageUrl(img)),
                    Caption = title
                };
                //add anything from
                if (textContent != null)
                {
                    media.Links.Add(textContent.Links);
                }
                //try and add links from any areas to it
                timelineWrapper.QuerySelectorAll("map area")
                    .ToList().ForEach(x => media.Links.Add(x));

                return media;

            }
            return null;
        }

        public static IEnumerable<MediaItem> ConvertGallery(IElement gallery)
        {
            List<MediaItem> ret = new List<MediaItem>();
            int imageNumber = 0;
            foreach(var galleryItem in gallery.QuerySelectorAll("li.gallerybox"))
            {
                imageNumber++;
                var media = ConvertImage(galleryItem, galleryItem.QuerySelector(".gallerytext"));
                if(media != null)
                {
                    //prefix it
                    media.Caption = $"Gallery {galleryNumber}, Image {imageNumber}: {media.Caption}";
                    ret.Add(media);
                }
            }
            galleryNumber++;
            return ret;
        }

        private static MediaItem ConvertImage(IElement imageContainer, IElement? captionContainer, string defaultText = "Article Image")
        {
            //some image holders can contain <canvas> graphs, charts, etc. So escape if you don't find an img
            var imgTag = imageContainer.QuerySelector("img");
            if (imgTag == null)
            {
                return null;
            }
            var url = CommonUtils.GetImageUrl(imgTag);
            if (url == null)
            {
                return null;
            }

            var description = GetImageDescrption(imageContainer, captionContainer, defaultText);
            var media = new MediaItem
            {
                Links = textExtractor.Links,
                Caption = description,
                Url = RouteOptions.MediaProxyUrl(url),
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

        public static IEnumerable<MediaItem> ConvertMontage(IElement tmulti, IElement? captionContainer = null)
        {
            List<MediaItem> ret = new List<MediaItem>();

            int imageNumber = 0;
            foreach (var thumb in tmulti.QuerySelectorAll(".thumbimage"))
            {
                imageNumber++;
                var media = ConvertImage(thumb, captionContainer);
                if (media != null)
                {
                    //prefix it
                    media.Caption = $"Montage {montageNumber}, Image {imageNumber}: {media.Caption}";
                    ret.Add(media);
                }
            }
            montageNumber++;
            return ret;
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

            var description = GetImageDescrption(imageContainer, captionContainer);

            return new VideoItem
            {
                Links = textExtractor.Links,
                Caption = description,
                Url = RouteOptions.MediaProxyUrl(imageUrl),
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

        /// <summary>
        /// Attempts to get an image's description using a caption element, alt text, or a default string
        /// </summary>
        /// <param name="imageContainer"></param>
        /// <param name="captionContainer"></param>
        /// <param name="defaultText"></param>
        /// <returns></returns>
        private static string GetImageDescrption(IElement imageContainer, IElement? captionContainer, string defaultText = "Article Image")
        {
            if (captionContainer != null)
            {
                //first see if there is a caption
                textExtractor.Extract(captionContainer);
                string text = textExtractor.Content;
                if (text.Length > 0)
                {
                    return text;
                }
            }
            //fall back to the ALT text
            string description = GetImageAlt(imageContainer);
            return (description.Length > 0) ? description : defaultText;
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
