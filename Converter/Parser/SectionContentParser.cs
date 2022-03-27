using System;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser
{
    public class SectionContentParser
    {
        ConverterSettings Settings;

        public SectionContentParser(ConverterSettings settings)
        {
            Settings = settings;
        }

        public SectionItem ParseElement(HtmlElement element)
        {
            switch(element.NodeName.ToLower())
            {
                case "div":
                    return ParseDiv(element);

                //high level content blocks we are ignoring
                case "table":
                    return null;

                default:
                    return ParseHtmlContent(element);
            }
        }

        private SectionItem ParseDiv(HtmlElement element)
        {
            //is it a media div?
            if (element.ClassList.Contains("thumb") && !element.ClassList.Contains("locmap"))
            {
                var url = element.QuerySelector("img")?.GetAttribute("src") ?? "";
                var caption = CommonUtils.PrepareTextContent(element.QuerySelector("div.thumbcaption")?.TextContent ?? "");
                if (url.Length > 0 && caption.Length > 0 && Settings.ShouldConvertMedia)
                {
                    return new MediaItem
                    {
                        Url = CommonUtils.RewriteMediaUrl(url),
                        Caption = caption
                    };
                }
            }

            //a navigation note?
            if (element.GetAttribute("role") == "note" && element.ClassList.Contains("navigation-not-searchable"))
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
            }

            //TODO: Handle Table inside naked Div

            return null;
        }

        private SectionItem ParseHtmlContent(HtmlElement element)
        {
            HtmlTranslater translater = new HtmlTranslater();

            var contents = translater.RenderHtml(element);
            if (contents.Length > 0)
            {

                //todo add support for extracting links from the HtmlRenderer instance
                //and add them to the section item

                return new ContentItem
                {
                    Content = contents,
                    LinkedArticles = translater.LinkedArticles
                };
            }
            return null;
        }
    }
}
