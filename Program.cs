using System;
using System.IO;
using System.Net;
using WikiProxy.API;
using WikiProxy.Converter;

using Gemini.Cgi;

namespace WikiProxy
{
    class Program
    {
        static void LocalTesting()
        {
            var title = "Alabama–Florida League";

            var client = new WikipediaApiClient();

            var resp = client.GetArticle(title);

            var newConverter = new StreamingWikiConverter(Console.Out);
            newConverter.ParseHtml(resp.Title, resp.HtmlText);

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
            router.OnRequest("/error", Error);
            router.OnRequest("/media", ProxyMedia);

            router.ProcessRequest();

        }

        static void Search(CgiWrapper cgi)
        {
            if(!cgi.HasQuery)
            {
                cgi.Input("Search for an Article");
                return;
            }

            cgi.Success();
            var client = new WikipediaApiClient();
            var searchResults = client.Search(cgi.SantiziedQuery);
            if (searchResults.Count == 0)
            {
                cgi.Writer.WriteLine($"No results found for '{cgi.SantiziedQuery}'.");
                cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/search Search Again");
                return;
            }
            int counter = 0;
            foreach(var result in searchResults)
            {
                counter++;
                cgi.Writer.WriteLine($"=> /cgi-bin/wp.cgi/view?{WebUtility.UrlEncode(result.Title)} {counter}. {result.Title}");
            }
        }

        static void Error(CgiWrapper cgi)
        {
            cgi.Success();
            cgi.Writer.WriteLine("We could access that article.");
            cgi.Writer.WriteLine("=> /cgi-bin/wp.cgi/search Search for an article");
        }

        static void ViewArticle(CgiWrapper cgi)
        {
            var client = new WikipediaApiClient();
            var resp = client.GetArticle(cgi.SantiziedQuery);

            if(resp == null)
            {
                cgi.Missing("Could not locate article");
                return;
            }
            cgi.Success();
            StreamingWikiConverter converter = new StreamingWikiConverter(cgi.Writer);
            converter.ParseHtml(resp.Title, resp.HtmlText);

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
    }
}
