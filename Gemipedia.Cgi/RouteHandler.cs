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
	public static class RouteHandler
	{

        #region Routes

        public static void Search(CgiWrapper cgi)
        {
            if (!cgi.HasQuery)
            {
                cgi.Input("Search for an Article");
                return;
            }

            cgi.Success();
            cgi.Writer.WriteLine($"Articles containing '{cgi.SantiziedQuery}'.");
            var searchResults = client.Search(cgi.SantiziedQuery);
            if (searchResults.Count == 0)
            {
                //TODO use "suggest API here
                cgi.Writer.WriteLine("No results found.");
                return;
            }
            else
            {
                int counter = 0;
                foreach (var result in searchResults)
                {
                    counter++;
                    cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl(result.Title)} {counter}. {result.Title}");
                    if (!string.IsNullOrEmpty(result.ThumbnailUrl))
                    {
                        cgi.Writer.WriteLine($"=> {RouteOptions.MediaProxyUrl(result.ThumbnailUrl)} Featured Image: {result.Title}");
                    }
                    cgi.Writer.WriteLine($">{ result.SummaryText}");
                    cgi.Writer.WriteLine();
                }
            }
            RenderFooter(cgi);
        }

        public static void SearchLatLon(CgiWrapper cgi)
        {
            if(!cgi.HasQuery)
            {
                cgi.Redirect("/cgi-bin/wp.cgi/");
                return;
            }

            var query = HttpUtility.ParseQueryString(cgi.Query);

            cgi.Success();

            var lat = Convert.ToDouble(query["lat"] ?? "0");
            var lon = Convert.ToDouble(query["lon"] ?? "0");
            var title = query["title"] ?? "";

            cgi.Writer.WriteLine($"Articles near '{title}'");

            var searchResults = client.GeoSearch(lat, lon);
            if (searchResults.Count == 0)
            {
                cgi.Writer.WriteLine("No results found.");
                return;
            }
            else
            {
                int counter = 0;
                foreach (var result in searchResults)
                {
                    counter++;
                    cgi.Writer.WriteLine($"=> /cgi-bin/wp.cgi/view?{WebUtility.UrlEncode(result.Title)} {counter}. {result.Title}");
                    cgi.Writer.WriteLine($"* Distance away: {result.Distance} m");
                    cgi.Writer.WriteLine();
                }
            }
            RenderFooter(cgi);
        }

        public static void Welcome(CgiWrapper cgi)
        {
            cgi.Success();
            cgi.Writer.WriteLine("# Gemipedia");
            cgi.Writer.WriteLine("Welcome to Gemipedia: A Gemini frontend to Wikipedia, focused on providing a 1st class reading experience.");
            cgi.Writer.WriteLine("");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/view Go to Article");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/search Search for Articles containing a phrase");
            cgi.Writer.WriteLine("");

            cgi.Writer.WriteLine("## Featured Content");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/featured Featured Article and 25 most popular articles (updated daily)");

            cgi.Writer.WriteLine("## Article Examples:");
            cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl("History of Apple Inc.")} History of Apple Inc.");
            cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl("Blue Poles")} Blue Poles");
            cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl("Gemini (protocol)")} Gemini (protocol)");
            cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl("Computer network")} Computer network");
            cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl("Interface Message Processor")} Interface Message Processor");
            cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl("ALOHAnet")} ALOHAnet");
        }

        public static void ViewFeatured(CgiWrapper cgi)
        {
            cgi.Success();

            cgi.Writer.WriteLine($"# Gemipedia Featured Content {DateTime.Now.ToString("yyyy-MM-dd")}");
            cgi.Writer.WriteLine("Compelling content pulled every day from the from page of Wikipedia");

            cgi.Writer.WriteLine("## Daily Featured Article");

            var featured = client.GetFeaturedContent();

            if (featured.FeaturedArticle != null)
            {
                cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl(featured.FeaturedArticle.Title)} {featured.FeaturedArticle.Title}");
                if (!string.IsNullOrEmpty(featured.FeaturedArticle.ThumbnailUrl))
                {
                    cgi.Writer.WriteLine($"=> {RouteOptions.MediaProxyUrl(featured.FeaturedArticle.ThumbnailUrl)} Featured Image: {featured.FeaturedArticle.Title}");
                }
                cgi.Writer.WriteLine($">{ featured.FeaturedArticle.Excerpt}");
                cgi.Writer.WriteLine();
            }
            else
            {
                cgi.Writer.WriteLine("(Featured article was unavailable)");
            }

            cgi.Writer.WriteLine("### 25 most viewed articles on Wikipedia today");

            if (featured.PopularArticles.Count > 0)
            {
                int counter = 0;
                foreach (var article in featured.PopularArticles)
                {
                    counter++;
                    cgi.Writer.WriteLine($"=> {RouteOptions.ArticleUrl(article.Title)} {counter}. {article.Title}");
                    if (!string.IsNullOrEmpty(article.ThumbnailUrl))
                    {
                        cgi.Writer.WriteLine($"=> {RouteOptions.MediaProxyUrl(article.ThumbnailUrl)} Featured Image: {article.Title}");
                    }
                    cgi.Writer.WriteLine($">{article.SummaryText}");
                    cgi.Writer.WriteLine();
                }
            } else
            {
                cgi.Writer.WriteLine("(Daily popular articles were unavailable)");
            }
            RenderFooter(cgi);
        }

        public static void ViewGeo(CgiWrapper cgi)
        {
            GeohackParser geoparser = null;
            try
            {
                geoparser = new GeohackParser(cgi.Query);
            }
            catch (Exception)
            {
                cgi.BadRequest("Invalid geo information");
                return;
            }
            cgi.Success();

            var renderer  = new GeoRenderer();
            renderer.RenderGeo(geoparser, cgi.Writer);
            RenderFooter(cgi);
        }

        public static void ViewArticle(CgiWrapper cgi)
        {
            if (!cgi.HasQuery)
            {
                cgi.Input("Article Name? (doesn't need to be exact)");
                return;
            }

            var resp = GetArticle(cgi);
            try
            {
                if (resp != null)
                {
                    if (RedirectParser.IsArticleRedirect(resp.HtmlText))
                    {
                        cgi.Redirect($"/cgi-bin/wp.cgi/view?{WebUtility.UrlEncode(RedirectParser.GetRedirectTitle(resp.HtmlText))}");
                        return;
                    }

                    cgi.Success();

                    var parsedPage = ParsePage(resp);
                    RenderArticle(parsedPage, cgi.Writer);
                }
                else
                {
                    //redirect to search...
                    cgi.Redirect($"/cgi-bin/wp.cgi/search?{cgi.RawQuery}");
                    return;
                }
            }
            catch (Exception ex)
            {
                cgi.Writer.WriteLine("Boom! Hit Exception!");
                cgi.Writer.WriteLine("```");
                cgi.Writer.WriteLine(ex.Message);
                cgi.Writer.WriteLine(ex.Source);
                cgi.Writer.WriteLine(ex.StackTrace);
                cgi.Writer.WriteLine("```");
            }
            RenderFooter(cgi);
        }

        public static void ViewImages(CgiWrapper cgi)
        {
            var resp = GetArticle(cgi);

            if (resp != null)
            {
                if (RedirectParser.IsArticleRedirect(resp.HtmlText))
                {
                    cgi.Redirect($"/cgi-bin/wp.cgi/images?{WebUtility.UrlEncode(RedirectParser.GetRedirectTitle(resp.HtmlText))}");
                    return;
                }

                cgi.Success();
                var page = ParsePage(resp);
                var gallery = new GalleryRenderer();
                gallery.RenderGallery(page, cgi.Writer);
            }
            else
            {
                cgi.Success();
                cgi.Writer.WriteLine("We could not access that article");
            }
            RenderFooter(cgi);
        }

        public static void ViewRefs(CgiWrapper cgi)
        {
            var query = HttpUtility.ParseQueryString(cgi.RawQuery);
            var title = query["name"] ?? "";
            var section = Convert.ToInt32(query["section"] ?? "-1");

            var resp = GetArticle(title);

            if (resp != null)
            {
                cgi.Success();
                var page = ParsePage(resp);
                var refs = new ReferencesRenderer(CommonUtils.Settings);
                refs.RenderReferences(page, cgi.Writer, section);
            }
            else
            {
                cgi.Success();
                cgi.Writer.WriteLine("We could not access that article");
            }
            RenderFooter(cgi);
        }

        public static void ProxyMedia(CgiWrapper cgi)
        {
            var url = cgi.Query;
            if (!IsSafeMediaUrl(url))
            {
                cgi.Missing("cannot fetch media");
                return;
            }
            MediaContent media = MediaProcessor.ProcessImage(client.GetMedia(url));
            cgi.Success(media.MimeType);
            cgi.Out.Write(media.Data);
        }


        #endregion

        static WikipediaApiClient client = new WikipediaApiClient();

        private static Article GetArticle(CgiWrapper cgi)
            => GetArticle(cgi.SantiziedQuery);

        private static Article GetArticle(string title)
            => client.GetArticle(title);

        private static ParsedPage ParsePage(Article resp)
        {
            var newConverter = new WikiHtmlConverter(CommonUtils.Settings);
            return newConverter.Convert(resp.Title, resp.HtmlText);
        }

        private static void RenderArticle(ParsedPage page, TextWriter output)
        {
            var renderer = new ArticleRenderer(CommonUtils.Settings);
            renderer.RenderArticle(page, Console.Out);
        }

        static bool IsSafeMediaUrl(string url)
        {
            try
            {
                var host = (new Uri(url)).Host; ;
                return host == "wikimedia.org" || host.EndsWith(".wikimedia.org");
            }
            catch (Exception)
            { }

            return false;
        }

        static void RenderFooter(CgiWrapper cgi)
        {
            cgi.Writer.WriteLine();
            cgi.Writer.WriteLine("--");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/ Gemipedia Home");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/view Go to Article");
            cgi.Writer.WriteLine($"=> mailto:acidus@gemi.dev?subject=Gemipedia+issue&body=URL%3A{WebUtility.UrlEncode(cgi.RequestUrl.ToString())} 🐛Report Bug");
            cgi.Writer.WriteLine("All content licensed under CC BY-SA 3.0");
        }
    }
}