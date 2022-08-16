using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Gemipedia.API;
using Gemipedia.API.Models;
using Gemipedia.Converter;
using Gemipedia.Converter.Special;
using Gemipedia.Models;
using Gemipedia.Media;
using Gemipedia.Renderer;


namespace Gemipedia.Console
{
    class Program
    {
        static ThreadSafeCounter counter = new ThreadSafeCounter();

        static void Main(string[] args)
        {
            StressTest();

            do {
                System.Console.WriteLine("Article?");
                string name = System.Console.ReadLine();
                if(name == "")
                {
                    return;
                }
                var article = GetArticle(name);
                var newConverter = new WikiHtmlConverter();

                ParsedPage page = newConverter.Convert(article.Title, article.HtmlText);

                var renderer = new ArticleRenderer();
                renderer.RenderArticle(page, System.Console.Out);


            } while (true) ;
        }
        
        static void StressTest()
        {
            int workers = 10;
            for(int i =0; i < workers; i++)
            {
                var thread = new Thread(new ThreadStart(DoStressWork));
                thread.Start();
            }

            while(true)
            {
                Thread.Sleep(30000);
            }
        }

        static void DoStressWork()
        {
            var converter = new WikiHtmlConverter();

            var client = new WikipediaApiClient(UserOptions.WikipediaVersion);

            while (true)
            {
                var count = counter.Increment();
                var title = client.GetRandomArticleTitle();

                try
                {
                    var article = GetArticle(title);
                    System.Console.WriteLine($"{count}\t{title}");
                    ParsedPage page = converter.Convert(article.Title, article.HtmlText);

                    StringWriter fout = new StringWriter();
                    var renderer = new ArticleRenderer();
                    renderer.RenderArticle(page, fout);
                } catch(Exception ex)
                {
                    System.IO.File.AppendAllText("/Users/billy/tmp/ERRORS.txt", $"\"{title}\"\t{ex.Message}\n");
                }
                System.Threading.Thread.Sleep(100);
            }
        }


        static Article GetArticle(string title)
        {

            var client = new WikipediaApiClient(UserOptions.WikipediaVersion);
            Article ret;

            bool gotArticle = true;
            do
            {
                gotArticle = true;
                ret = client.GetArticle(title);

                if (RedirectParser.IsArticleRedirect(ret.HtmlText))
                {
                    gotArticle = false;
                    title = RedirectParser.GetRedirectTitle(ret.HtmlText);
                }
            } while (!gotArticle);

            return ret;
        }

       

    }
}

