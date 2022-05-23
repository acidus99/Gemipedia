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
            CommonUtils.Settings = DefaultSettings;

            var title = "F101";

            var article = GetArticle(title);
            var newConverter = new WikiHtmlConverter(CommonUtils.Settings);

            ParsedPage page = newConverter.Convert(article.Title, article.HtmlText);

            var renderer = new ArticleRenderer(CommonUtils.Settings);
            renderer.RenderArticle(page, System.Console.Out);

        }

        static Article GetArticle(string title)
        {

            var client = new WikipediaApiClient();
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

        static ConverterSettings DefaultSettings
            => new ConverterSettings
            {
                ExcludedSections = new string[] { "bibliography", "citations", "external_links", "notes", "references", "further_reading" },
                ArticleLinkSections = new string[] { "see also" },
                ArticleUrl = "/cgi-bin/wp.cgi/view",
                ImageGallerUrl = "/cgi-bin/wp.cgi/images",
                MediaProxyUrl = "/cgi-bin/wp.cgi/media/media",
                ReferencesUrl = "/cgi-bin/wp.cgi/refs",
                SearchUrl = "/cgi-bin/wp.cgi/search",
            };

    }
}

