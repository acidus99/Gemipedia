using System;
using System.IO;
using System.Net;
using System.Web;

using Gemini.Cgi;
using Gemipedia.API;
using Gemipedia.API.Models;
using Gemipedia.Converter;
using Gemipedia.Converter.Special;
using Gemipedia.Models;
using Gemipedia.Media;
using Gemipedia.Renderer;

namespace Gemipedia.Cgi
{
    class Program
    {

        static void Main(string[] args)
        {

            CommonUtils.Settings = DefaultSettings;

            CgiRouter router = new CgiRouter();
            router.OnRequest("/search", RouteHandler.Search);
            router.OnRequest("/view", RouteHandler.ViewArticle);
            router.OnRequest("/images", RouteHandler.ViewImages);
            router.OnRequest("/media", RouteHandler.ProxyMedia);
            router.OnRequest("/refs", RouteHandler.ViewRefs);
            router.OnRequest("/featured", RouteHandler.ViewFeatured);
            router.OnRequest("", RouteHandler.Welcome);
            router.ProcessRequest();
        }

        static ConverterSettings DefaultSettings
            => new ConverterSettings
            {
                ExcludedSections = new string []{ "bibliography", "citations", "external_links", "notes", "references", "further_reading" },
                ArticleLinkSections = new string[] {"see also"},
                ArticleUrl = "/cgi-bin/wp.cgi/view",
                ImageGallerUrl = "/cgi-bin/wp.cgi/images",
                MediaProxyUrl = "/cgi-bin/wp.cgi/media/media",
                ReferencesUrl = "/cgi-bin/wp.cgi/refs",
                SearchUrl = "/cgi-bin/wp.cgi/search",
            };
    }
}
