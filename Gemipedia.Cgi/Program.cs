using System;
using System.Text.RegularExpressions;

using Gemini.Cgi;
using Gemipedia;

namespace Gemipedia.Cgi
{
    class Program
    {
        private static readonly Regex regexLang = new Regex("([a-z]{2})", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            SetPaths();

            CgiRouter router = new CgiRouter(ParseWikiLanguage);
            router.OnRequest("/search", RouteHandler.Search);
            router.OnRequest("/view", RouteHandler.ViewArticle);
            router.OnRequest("/images", RouteHandler.ViewImages);
            router.OnRequest("/media", RouteHandler.ProxyMedia);
            router.OnRequest("/refs", RouteHandler.ViewRefs);
            router.OnRequest("/featured", RouteHandler.ViewFeatured);
            router.OnRequest("/geo", RouteHandler.ViewGeo);
            router.OnRequest("/latlon", RouteHandler.SearchLatLon);
            router.OnRequest("/lang", RouteHandler.SelectLanguage);
            router.OnRequest("/otherlang", RouteHandler.ViewOtherLanguages);
            router.OnRequest("/", RouteHandler.Welcome);
            router.OnRequest("", RouteHandler.Welcome);
            router.ProcessRequest();
        }

        static void ParseWikiLanguage(CgiWrapper cgi)
        {
            var parts = cgi.PathInfo.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && regexLang.IsMatch(parts[1]))
            {
                UserOptions.WikipediaVersion = parts[1].ToLower();
            }
        }

        static void SetPaths()
        {
            RouteOptions.BaseArticleUrl = "/cgi-bin/wp.cgi/view";
            RouteOptions.BaseFeaturedContenteUrl = "/cgi-bin/wp.cgi/featured";
            RouteOptions.BaseGeoUrl = "/cgi-bin/wp.cgi/geo";
            RouteOptions.BaseImageGallerUrl = "/cgi-bin/wp.cgi/images";
            RouteOptions.BaseLanguageUrl = "/cgi-bin/wp.cgi/lang";
            RouteOptions.BaseLonLatUrl = "/cgi-bin/wp.cgi/latlon";
            RouteOptions.BaseMediaProxyUrl = "/cgi-bin/wp.cgi/media/media";
            RouteOptions.BaseOtherLanguagesUrl = "/cgi-bin/wp.cgi/otherlang";
            RouteOptions.BaseReferencesUrl = "/cgi-bin/wp.cgi/refs";
            RouteOptions.BaseSearchUrl = "/cgi-bin/wp.cgi/search";
            RouteOptions.BaseWelcomeUrl = "/cgi-bin/wp.cgi/welcome";
        }
    }
}
