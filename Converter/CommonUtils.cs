using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using System.Text;
using System.Text.RegularExpressions;

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


        /// <summary>
        /// Gets a properly formatted image URL from an IMG object 
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static string GetImageUrl(IElement img)
        {
            //try the srcset
            var url = GetImageFromSrcset(img.GetAttribute("srcset") ?? "", "2x");
            if (url != null)
            {
                return EnsureHttps(url);
            }
            return EnsureHttps(img.GetAttribute("src") ?? null);
        }

        public static string EnsureHttps(string url)
           => (url != null && !url.StartsWith("https:")) ?
               "https:" + url :
               url;

        private static string GetImageFromSrcset(string srcset, string size)
        {
            if (srcset.Length > 0)
            {
                Regex parser = new Regex(@"(\S*[^,\s])(\s+([\d.]+)(x|w))?");

                return parser.Matches(srcset)
                    .Where(x => x.Success && x.Groups[2].Value.Trim() == size)
                    .Select(x => x.Groups[1].Value).FirstOrDefault() ?? null;
            }
            return null;
        }


    }
}
