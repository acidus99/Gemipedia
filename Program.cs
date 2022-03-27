using System;
using System.IO;
using System.Net;
using Gemipedia.API;
using Gemipedia.Converter;
using System.Diagnostics;

using Gemini.Cgi;

namespace Gemipedia
{
    class Program
    {
        static void LocalTesting()
        {
            //var title = "Minor League Baseball";
            var title = "McDonnell F-101 Voodoo";
            //var title = "Pet door";

            var client = new WikipediaApiClient();

            var resp = client.GetArticle(title);

            //new output
            StreamWriter fout = new StreamWriter("/Users/billy/tmp/output-new.gmi");
            Stopwatch newTimer = new Stopwatch();
            newTimer.Start();
            var newConverter = new NewConverter(DefaultSettings);
            newConverter.Convert(resp.Title, resp.HtmlText, fout);
            newTimer.Stop();
            fout.Close();


            ////legacy output
            //fout = new StreamWriter("/Users/billy/tmp/output-legacy.gmi");
            //Stopwatch legacyTimer = new Stopwatch();
            //legacyTimer.Start();
            //var legacyConverter = new WikiHtmlConverter(DefaultSettings);
            //legacyConverter.Convert(fout, resp.Title, resp.HtmlText);
            //legacyTimer.Stop();
            //fout.Close();

            int x = 4;
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
            router.OnRequest("/media", ProxyMedia);
            router.OnRequest("/lucky", Lucky);
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
            RenderFooter(cgi.Writer);
        }

        static void Lucky(CgiWrapper cgi)
        {
            if (!cgi.HasQuery)
            {
                cgi.Input("Article Name? (doesn't need to be exact)");
                return;
            }
            cgi.Redirect($"/cgi-bin/wp.cgi/view?{cgi.RawQuery}");
        }

        static void Welcome(CgiWrapper cgi)
        {
            cgi.Success();
            cgi.Writer.WriteLine("# Gemipedia");
            cgi.Writer.WriteLine("Welcome to Gemipedia: A Gemini proxy to Wikipedia, focused on providing a 1st class reading experience.");
            cgi.Writer.WriteLine("");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/lucky Go to Article");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/search Search");
        }

        static void ViewArticle(CgiWrapper cgi)
        {
            var client = new WikipediaApiClient();
            var resp = client.GetArticle(cgi.SantiziedQuery);

            if (resp != null)
            {
                if(RedirectParser.IsArticleRedirect(resp.HtmlText))
                {
                    cgi.Redirect($"/cgi-bin/wp.cgi/view?{WebUtility.UrlEncode(RedirectParser.GetRedirectTitle(resp.HtmlText))}");
                    return;
                }

                cgi.Success();
                //var converter = new WikiHtmlConverter(DefaultSettings);
                var converter = new NewConverter(DefaultSettings);
                converter.Convert(resp.Title, resp.HtmlText, cgi.Writer);
            }
            else
            {
                cgi.Success();
                cgi.Writer.WriteLine("We could not access that article");
            }
            RenderFooter(cgi.Writer);
        }

        static void ProxyMedia(CgiWrapper cgi)
        {
            var url = cgi.Query;
            if (!url.StartsWith("//upload.wikimedia.org/"))
            {
                cgi.Missing("cannot fetch media");
                return;
            }
            var client = new WikipediaApiClient();
            cgi.Success("image/jpeg");
            cgi.Out.Write(client.FetchMedia("https:" + url));
        }

        static void RenderFooter(TextWriter tw)
        {
            tw.WriteLine();
            tw.WriteLine("--");
            tw.WriteLine("=> /cgi-bin/wp.cgi/lucky Go to Article");
            tw.WriteLine("=> /cgi-bin/wp.cgi/search Search Wikipedia");
        }

        static ConverterSettings DefaultSettings
            => new ConverterSettings
            {
                ArticleUrl = "/cgi-bin/wp.cgi/view",
                ExcludedSections = new string []{ "bibliography", "citations", "external_links", "notes", "references" },
                MediaProxyUrl = "/cgi-bin/wp.cgi/media/thumb.jpg",
            };
    }
}
