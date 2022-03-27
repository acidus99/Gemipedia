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

        public static bool IsHeader(INode node)
            => node.NodeType == NodeType.Element &&
                node.NodeName.Length == 2 &&
                node.NodeName[0] == 'H' &&
                char.IsDigit(node.NodeName[1]);

        public static string GetHeaderText(INode node)
            => ((HtmlElement)node).QuerySelector("span.mw-headline").TextContent.Trim().Replace("\n", "");

        public static string RewriteMediaUrl(string url)
         => $"{Settings.MediaProxyUrl}?{WebUtility.UrlEncode(url)}";

        public static string ArticleUrl(string title)
            => $"{Settings.ArticleUrl}?{WebUtility.UrlEncode(title)}";

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
