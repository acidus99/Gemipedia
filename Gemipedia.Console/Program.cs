using System;
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
        static void Main(string[] args)
        {

            var title = (args.Length > 0) ? args[0] : "Statue of Liberty";

            var article = GetArticle(title);
            var newConverter = new WikiHtmlConverter();

            ParsedPage page = newConverter.Convert(article.Title, article.HtmlText);

            var renderer = new ArticleRenderer();
            renderer.RenderArticle(page, System.Console.Out);

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

