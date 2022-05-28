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

namespace Gemipedia
{
    public static class CommonUtils
    {
        static public ConverterSettings Settings { get;  set; }

        public static string ArticleUrl(string title)
            => $"{Settings.ArticleUrl}?{WebUtility.UrlEncode(title)}";

        public static string GeoUrl(string geohackUrl)
            => $"{Settings.ArticleUrl}?{WebUtility.UrlEncode(geohackUrl)}";


        public static string ImageGalleryUrl(string title)
            => $"{Settings.ImageGallerUrl}?{WebUtility.UrlEncode(title)}";

        public static string MediaProxyUrl(string url)
        {
            //we need to have an extension on the filename of the media proxy URL, so clients
            //will render it as an inline image. Try and figure out what to use, but fall back
            //to a dummy "jpg" if nothing works
            string ext = ".jpg";
            try
            {
                var uri = new Uri(url);
                ext = Path.GetExtension(uri.AbsolutePath);
            }
            catch (Exception)
            {
                ext = ".jpg";
            }
            return $"{Settings.MediaProxyUrl}{ext}?{WebUtility.UrlEncode(url)}";
        }

        public static string PdfUrl(string escapedTitle)
            => $"https://en.wikipedia.org/api/rest_v1/page/pdf/{WebUtility.UrlEncode(escapedTitle)}";

        public static string ReferencesUrl(string title)
             => $"{Settings.ReferencesUrl}?name={WebUtility.UrlEncode(title)}";

        public static string ReferencesUrl(string title, int sectionNum)
             => $"{Settings.ReferencesUrl}?name={WebUtility.UrlEncode(title)}&section={sectionNum}";

        public static string SearchUrl(string query)
            => $"{Settings.SearchUrl}?{WebUtility.UrlEncode(query)}";

        public static string WikipediaSourceUrl(string escapedTitle)
            => $"https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(escapedTitle)}";

        public static string PrepareTextContent(string s)
            => s.Trim().Replace("\n", "");


        /// <summary>
        /// Gets a properly formatted image URL from an IMG object 
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static string GetImageUrl(IElement img)
        {
            //try srcset 2x
            var url = GetImageFromSrcset(img?.GetAttribute("srcset") ?? "", "2x");
            if (!string.IsNullOrEmpty(url))
            {
                return EnsureHttps(url);
            }
            //try srcset 1.5
            url = GetImageFromSrcset(img?.GetAttribute("srcset") ?? "", "1.5x");
            if (!string.IsNullOrEmpty(url))
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
