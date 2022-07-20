using System;
using System.IO;
using System.Net;

namespace Gemipedia
{
    public static class RouteOptions
    {
        #region base URLs

        /// <summary>
        /// Base URL to use to view an article. Actual artical passed via query string
        /// </summary>
        public static string BaseArticleUrl { get; set; }

        /// <summary>
        /// BaseURL to use to view geographic data.
        /// </summary>
        public static string BaseGeoUrl { get; set; }

        public static string BaseImageGallerUrl { get; set; }

        /// <summary>
        /// URL to use to proxy media. actual media path passed via query string
        /// </summary>
        public static string BaseMediaProxyUrl { get; set; }

        public static string BaseReferencesUrl { get; set; }

        public static string BaseSearchUrl { get; set; }

        public static string BaseWelcomeUrl { get; set; }

        #endregion

        public static string ArticleUrl(string title)
           => $"{AddLanguage(BaseArticleUrl)}?{WebUtility.UrlEncode(title)}";

        public static string GeoUrl(string geohackUrl)
            => $"{AddLanguage(BaseGeoUrl)}?{WebUtility.UrlEncode(geohackUrl)}";

        public static string ImageGalleryUrl(string title)
            => $"{AddLanguage(BaseImageGallerUrl)}?{WebUtility.UrlEncode(title)}";

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
            return $"{BaseMediaProxyUrl}{ext}?{WebUtility.UrlEncode(url)}";
        }

        public static string PdfUrl(string escapedTitle)
            => $"https://{UserOptions.WikipediaVersion}.wikipedia.org/api/rest_v1/page/pdf/{WebUtility.UrlEncode(escapedTitle)}";

        public static string ReferencesUrl(string title)
             => $"{AddLanguage(BaseReferencesUrl)}?name={WebUtility.UrlEncode(title)}";

        public static string ReferencesUrl(string title, int sectionNum)
             => $"{AddLanguage(BaseReferencesUrl)}?name={WebUtility.UrlEncode(title)}&section={sectionNum}";

        public static string SearchUrl(string query)
            => $"{AddLanguage(BaseSearchUrl)}?{WebUtility.UrlEncode(query)}";

        public static string WikipediaSourceUrl(string escapedTitle)
            => $"https://{UserOptions.WikipediaVersion}.wikipedia.org/wiki/{WebUtility.UrlEncode(escapedTitle)}";

        //if we can help it, avoid adding a language, since it increases the size of the URL
        //which can cause problems if we have to proxy something long
        private static string AddLanguage(string url)
            => (UserOptions.WikipediaVersion == "en") ? url : url + '/' + UserOptions.WikipediaVersion;
    }
}

