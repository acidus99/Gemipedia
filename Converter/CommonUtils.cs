using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using System.Text;

using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace Gemipedia.Converter
{
    internal static class CommonUtils
    {
        static public ConverterSettings Settings { get;  set; }



        public static string GetHeaderText(INode node)
            => ((HtmlElement)node).QuerySelector("span.mw-headline").TextContent.Trim().Replace("\n", "");

        public static string RewriteMediaUrl(string url)
         => $"{Settings.MediaProxyUrl}?{WebUtility.UrlEncode(url)}";

        public static string ArticleUrl(string title)
            => $"{Settings.ArticleUrl}?{WebUtility.UrlEncode(title)}";

        public static string ImageGalleryUrl(string title)
            => $"{Settings.ImageGallerUrl}?{WebUtility.UrlEncode(title)}";

        public static string PrepareTextContent(string s)
            => s.Trim().Replace("\n", "");

        public static bool ShouldUseLink(IElement element)
            => element.HasAttribute("title") &&
                //ignore links to special pages!
                !element.GetAttribute("title").StartsWith("Special:") &&
                //links to pages that don't exist have a "new" class
                !element.ClassList.Contains("new");
    }
}
