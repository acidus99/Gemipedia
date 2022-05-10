using System;
using System.IO;
using System.Net;
using System.Web;
using Gemipedia.API;

using Gemini.Cgi;
using Gemipedia.Converter;
using Gemipedia.Media;

namespace Gemipedia
{
    class Program
    {
        static void LocalTesting()
        {

            var title = "navy";
            Console.WriteLine(title);
            var client = new WikipediaApiClient();
            var resp = client.GetArticle(title);
            //test caching
            resp = client.GetArticle(title);

            var newConverter = new WikiHtmlConverter(DefaultSettings);
            newConverter.Convert(resp.Title, resp.HtmlText, Console.Out);
        }

        static void Main(string[] args)
        {
            if (!CgiWrapper.IsRunningAsCgi)
            {
                LocalTesting();
                return;
            }

            CgiRouter router = new CgiRouter();
            router.OnRequest("/search", Search);
            router.OnRequest("/view", ViewArticle);
            router.OnRequest("/images", ViewImages);
            router.OnRequest("/media", ProxyMedia);
            router.OnRequest("/refs", ViewRefs);
            router.OnRequest("", Welcome);
            router.ProcessRequest();
        }

        static void Search(CgiWrapper cgi)
        {
            if (!cgi.HasQuery)
            {
                cgi.Input("Search for an Article");
                return;
            }

            cgi.Success();
            cgi.Writer.WriteLine($"Results for '{cgi.SantiziedQuery}'.");
            var client = new WikipediaApiClient();
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
                    cgi.Writer.WriteLine($"=> /cgi-bin/wp.cgi/view?{WebUtility.UrlEncode(result.Title)} {counter}. {result.Title}");
                }
            }
            RenderFooter(cgi);
        }

        static void Welcome(CgiWrapper cgi)
        {
            CommonUtils.Settings = DefaultSettings;

            cgi.Success();
            cgi.Writer.WriteLine("# Gemipedia");
            cgi.Writer.WriteLine("Welcome to Gemipedia: A Gemini frontend to Wikipedia, focused on providing a 1st class reading experience.");
            cgi.Writer.WriteLine("");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/view Go to Article");
            cgi.Writer.WriteLine("");
            cgi.Writer.WriteLine("## Article Examples:");
            cgi.Writer.WriteLine($"=> {CommonUtils.ArticleUrl("History of Apple Inc.")} History of Apple Inc.");
            cgi.Writer.WriteLine($"=> {CommonUtils.ArticleUrl("Blue Poles")} Blue Poles");
            cgi.Writer.WriteLine($"=> {CommonUtils.ArticleUrl("Gemini (protocol)")} Gemini (protocol)");
            cgi.Writer.WriteLine($"=> {CommonUtils.ArticleUrl("Computer network")} Computer network");
            cgi.Writer.WriteLine($"=> {CommonUtils.ArticleUrl("Interface Message Processor")} Interface Message Processor");
            cgi.Writer.WriteLine($"=> {CommonUtils.ArticleUrl("ALOHAnet")} ALOHAnet");

        }

        static void ViewArticle(CgiWrapper cgi)
        {
            if (!cgi.HasQuery)
            {
                cgi.Input("Article Name? (doesn't need to be exact)");
                return;
            }

            var client = new WikipediaApiClient();
            var resp = client.GetArticle(cgi.SantiziedQuery);
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
                    var converter = new WikiHtmlConverter(DefaultSettings);
                    converter.Convert(resp.Title, resp.HtmlText, cgi.Writer);
                }
                else
                {
                    //redirect to search...
                    cgi.Redirect($"/cgi-bin/wp.cgi/search?{cgi.RawQuery}");
                    return;
                }
            } catch(Exception ex)
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

        static void ViewImages(CgiWrapper cgi)
        {
            var client = new WikipediaApiClient();
            var resp = client.GetArticle(cgi.SantiziedQuery);

            if (resp != null)
            {
                if (RedirectParser.IsArticleRedirect(resp.HtmlText))
                {
                    cgi.Redirect($"/cgi-bin/wp.cgi/images?{WebUtility.UrlEncode(RedirectParser.GetRedirectTitle(resp.HtmlText))}");
                    return;
                }

                cgi.Success();
                //var converter = new WikiHtmlConverter(DefaultSettings);
                var converter = new WikiHtmlConverter(DefaultSettings);
                converter.ConvertImageGallery(resp.Title, resp.HtmlText, cgi.Writer);
            }
            else
            {
                cgi.Success();
                cgi.Writer.WriteLine("We could not access that article");
            }
            RenderFooter(cgi);
        }

        static void ViewRefs(CgiWrapper cgi)
        {
            var client = new WikipediaApiClient();


            var query = HttpUtility.ParseQueryString(cgi.RawQuery);
            var title = query["name"] ?? "";
            var section = Convert.ToInt32(query["section"] ?? "-1");            

            var resp = client.GetArticle(title);

            if (resp != null)
            {
                cgi.Success();
                var converter = new WikiHtmlConverter(DefaultSettings);
                converter.ConvertReferences(resp.Title, resp.HtmlText, cgi.Writer, section);
            }
            else
            {
                cgi.Success();
                cgi.Writer.WriteLine("We could not access that article");
            }
            RenderFooter(cgi);
        }

        static void ProxyMedia(CgiWrapper cgi)
        {
            var url = cgi.Query;
            if (!IsSafeMediaUrl(url))
            {
                cgi.Missing("cannot fetch media");
                return;
            }

            Console.Error.WriteLine($"fetching '{url}'");

            var client = new WikipediaApiClient();

            MediaContent media = MediaProcessor.ProcessImage(client.GetMedia(url));
            cgi.Success(media.MimeType);
            cgi.Out.Write(media.Data);
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

        static ConverterSettings DefaultSettings
            => new ConverterSettings
            {
                ExcludedSections = new string []{ "bibliography", "citations", "external_links", "notes", "references", "further_reading" },
                ArticleLinkSections = new string[] {"see also"},
                ArticleUrl = "/cgi-bin/wp.cgi/view",
                MediaProxyUrl = "/cgi-bin/wp.cgi/media/media",
                ImageGallerUrl = "/cgi-bin/wp.cgi/images",
                ReferencesUrl = "/cgi-bin/wp.cgi/refs",
            };
    }
}
