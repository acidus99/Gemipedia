using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Parser.Tables;
using Gemipedia.Converter.Models;


namespace Gemipedia.Converter.Parser
{

    public static class SpecialBlockConverter
    {

        /// <summary>
        /// Attempts to convert an inline Math element into a linkable image
        /// Math formulas are in SVG, so link to our converter
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string ConvertMath(HtmlElement element)
        {
            var img = element.QuerySelector("img");
            var url = img?.GetAttribute("src") ?? "";
            var caption = img?.GetAttribute("alt") ?? "";

            if (url.Length > 0 && caption.Length > 0)
            {
                //not a media item, since it shouldn't be moved
                return $"\n=> {CommonUtils.SvgProxyUrl(url)} Math Formula: {CommonUtils.PrepareTextContent(caption)}\n";
            }
            return "";
        }

        /// <summary>
        /// Convert a navigation note in a section
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static SectionItem ConvertNavigationNotes(HtmlElement element)
        {
            var textExtractor = new TextExtractor(true);
            var text = textExtractor.GetText(element);
            return new NavSuggestionsItem
            {
                ArticleLinks = textExtractor.ArticleLinks,
                Content = text
            };
        }

        /// <summary>
        /// Convert a data table
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static SectionItem ConvertWikiTable(HtmlElement element)
        {
            //do we have a timeline?
            var media = ConvertTimelineInTable(element);
            if(media != null)
            {
                return media;
            }

            TableParser tableParser = new TableParser();
            var table = tableParser.ParseTable(element);

            var contents = TableRenderer.RenderTable(table);

            return new ContentItem
            {
                Content = contents,
                ArticleLinks = tableParser.ArticleLinks
            };
        }
        
        public static MediaItem ConvertTimelineInTable(IElement element)
        {
            var timeline = element.QuerySelector("div.timeline-wrapper");
            if(timeline != null)
            {
                TextExtractor textExtractor = new TextExtractor
                {
                    ShouldCollapseNewlines = true
                };

                //attempt to get a meaningful title for the timeline form the first
                //cell
                var content = textExtractor.GetText(element.QuerySelector("th"), element.QuerySelector("td"));

                var media = ConvertTimeline(timeline, content);
                if(media != null)
                {
                    media.ArticleLinks.MergeCollection(textExtractor.ArticleLinks);
                    return media;
                }
            }
            return null;
        }

        public static MediaItem ConvertTimeline(IElement timelineWrapper, string title = "")
        {
            var img = timelineWrapper.QuerySelector("img[usemap]");

            title = string.IsNullOrEmpty(title) ? "Timeline Image" : $"Timeline Image: {title}";

            if(img != null)
            {
                var media = new MediaItem
                {
                    Url = CommonUtils.MediaProxyUrl(img.GetAttribute("src")),
                    Caption = title
                };
                //try and add links from any areas to it
                timelineWrapper.QuerySelectorAll("map area")
                    .ToList().ForEach(x => media.ArticleLinks.Add(x));

                return media;

            }
            return null;
        }
    }
}
