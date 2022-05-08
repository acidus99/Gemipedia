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

        public static string MediaProxyUrl(string url)
            => RewriteMediaUrl(url, Settings.MediaProxyUrl);

        public static string SvgProxyUrl(string url)
            => RewriteMediaUrl(url, Settings.SvgProxyUrl);

        private static string RewriteMediaUrl(string url, string cgiPath)
            => $"{cgiPath}?{WebUtility.UrlEncode(url)}";

        public static string ArticleUrl(string title)
            => $"{Settings.ArticleUrl}?{WebUtility.UrlEncode(title)}";

        public static string ImageGalleryUrl(string title)
            => $"{Settings.ImageGallerUrl}?{WebUtility.UrlEncode(title)}";

        public static string PdfUrl(string escapedTitle)
            => $"https://en.wikipedia.org/api/rest_v1/page/pdf/{WebUtility.UrlEncode(escapedTitle)}";

        public static string WikipediaSourceUrl(string escapedTitle)
            => $"https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(escapedTitle)}";

        public static string ReferencesUrl(string title)
             => $"{Settings.ReferencesUrl}?name={WebUtility.UrlEncode(title)}";

        public static string ReferencesUrl(string title, int sectionNum)
             => $"{Settings.ReferencesUrl}?name={WebUtility.UrlEncode(title)}&section={sectionNum}";

        public static string PrepareTextContent(string s)
            => s.Trim().Replace("\n", "");
    }
}
