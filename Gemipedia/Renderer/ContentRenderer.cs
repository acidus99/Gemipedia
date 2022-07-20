using System;
using System.Linq;

using Gemipedia.Models;

namespace Gemipedia.Renderer
{
    public static class ContentRenderer
    {
        public static void RenderGeo(SimpleBuffer buffer, GeoItem geo)
        {
            buffer.EnsureAtLineStart();
            buffer.AppendLine($"=> {geo.Url} 🌍 {geo.Title}");
        }

        public static void RenderMedia(SimpleBuffer buffer, MediaItem media)
        {
            buffer.EnsureAtLineStart();

            if (media is VideoItem)
            {
                var video = (VideoItem)media;
                buffer.AppendLine($"=> {video.Url} Video Still: {video.Caption}");
                buffer.AppendLine($"=> {video.VideoUrl} Source Video: {video.VideoDescription}"); ;
            }
            else
            {
                buffer.AppendLine($"=> {media.Url} {media.Caption}");
            }
        }

        public static void RenderNavSuggestion(SimpleBuffer buffer, NavSuggestionsItem nav)
        {
            var links = nav.Links.GetLinks();
            if (links.Count == 1)
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine($"=> {RouteOptions.ArticleUrl(links[0])} {nav.Content}");
            }
            else
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine($"({nav.Content})");
                foreach (var linkTitle in links)
                {
                    buffer.AppendLine($"=> {RouteOptions.ArticleUrl(linkTitle)} {linkTitle}");
                }
            }
        }
    }
}
