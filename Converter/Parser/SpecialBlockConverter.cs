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

        public static SectionItem ConvertImageBlock(HtmlElement element)
        {
            var url = GetImageUrl(element);
            var caption = CommonUtils.PrepareTextContent(element.QuerySelector("div.thumbcaption")?.TextContent ?? "");
            if (url.Length > 0 && caption.Length > 0)
            {
                if (!url.StartsWith("https:"))
                {
                    url = "https:" + url;
                }

                return new MediaItem
                {
                    Url = CommonUtils.MediaProxyUrl(url),
                    Caption = caption
                };
            }
            return null;
        }

        private static string GetImageUrl(HtmlElement element)
        {
            //try the srcset
            var url = GetImageFromSrcset(element.QuerySelector("img")?.GetAttribute("srcset") ?? "", "2x");
            if(url.Length > 0)
            {
                return url;
            }
            return element.QuerySelector("img")?.GetAttribute("src") ?? "";
        }

        private static string GetImageFromSrcset(string srcset, string size)
        {
            if (srcset.Length > 0)
            {
                Regex parser = new Regex(@"(\S*[^,\s])(\s+([\d.]+)(x|w))?");

                return parser.Matches(srcset)
                    .Where(x => x.Success && x.Groups[2].Value.Trim() == size)
                    .Select(x => x.Groups[1].Value).FirstOrDefault();
            }
            return "";
        }


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
            var lines = element.TextContent.Split(".").Where(x => x.Trim().Length > 0).ToArray();
            var tags = element.QuerySelectorAll("a");
            if (lines.Length == tags.Length)
            {
                var item = new NavSuggestionsItem();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (CommonUtils.ShouldUseLink(tags[i]))
                    {
                        item.Suggestions.Add(new NavSuggestion
                        {
                            ArticleTitle = CommonUtils.ArticleUrl(tags[i].GetAttribute("title")),
                            Description = $"{CommonUtils.PrepareTextContent(lines[i])}."
                        });
                    }
                }
                return (item.Suggestions.Count > 0) ? item : null;
            }
            return null;
        }

        /// <summary>
        /// Convert a data table
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static ContentItem ConvertTable(HtmlElement element)
        {
            TableParser tableParser = new TableParser();
            var table = tableParser.ParseTable(element);

            var contents = TableRenderer.RenderTable(table);

            return new ContentItem
            {
                Content = contents,
                LinkedArticles = tableParser.LinkedArticles
            };
        }

    }
}
