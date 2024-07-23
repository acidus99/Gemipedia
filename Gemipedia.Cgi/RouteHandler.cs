using System;
using System.IO;
using System.Web;
using Gemini.Cgi;
using Gemipedia.API;
using Gemipedia.API.Models;
using Gemipedia.Converter;
using Gemipedia.Converter.Special;
using Gemipedia.Media;
using Gemipedia.Models;
using Gemipedia.Renderer;

namespace Gemipedia.Cgi;

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

        cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
        var outWriter = new CountingTextWriter(cgi.Writer);

        outWriter.WriteLine($"Articles containing '{cgi.SantiziedQuery}'.");
        var searchResults = client.Search(cgi.SantiziedQuery);
        if (searchResults.Count == 0)
        {
            //TODO use "suggest API here
            outWriter.WriteLine("No results found.");
            return;
        }
        else
        {
            int counter = 0;
            foreach (var result in searchResults)
            {
                counter++;
                outWriter.WriteLine($"=> {RouteOptions.ArticleUrl(result.Title)} {counter}. {result.Title}");
                if (!string.IsNullOrEmpty(result.ThumbnailUrl))
                {
                    outWriter.WriteLine($"=> {RouteOptions.MediaProxyUrl(result.ThumbnailUrl)} Featured Image: {result.Title}");
                }
                outWriter.WriteLine($">{result.SummaryText}");
                outWriter.WriteLine();
            }
        }
        RenderFooter(outWriter, client.DownloadTimeMs);
    }

    public static void SearchLatLon(CgiWrapper cgi)
    {
        if (!cgi.HasQuery)
        {
            cgi.Redirect(RouteOptions.WelcomeUrl());
            return;
        }

        var query = HttpUtility.ParseQueryString(cgi.Query);

        cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
        var outWriter = new CountingTextWriter(cgi.Writer);

        var lat = Convert.ToDouble(query["lat"] ?? "0");
        var lon = Convert.ToDouble(query["lon"] ?? "0");
        var title = query["title"] ?? "";

        outWriter.WriteLine($"Articles near '{title}'");

        var searchResults = client.GeoSearch(lat, lon);
        if (searchResults.Count == 0)
        {
            outWriter.WriteLine("No results found.");
            return;
        }
        else
        {
            int counter = 0;
            foreach (var result in searchResults)
            {
                counter++;
                outWriter.WriteLine($"=> {RouteOptions.ArticleUrl(result.Title)} {counter}. {result.Title}");
                outWriter.WriteLine($"* Distance away: {result.Distance} m");
                outWriter.WriteLine();
            }
        }
        RenderFooter(outWriter, client.DownloadTimeMs);
    }

    public static void SelectLanguage(CgiWrapper cgi)
    {
        cgi.Success();
        var outWriter = new CountingTextWriter(cgi.Writer);

        outWriter.WriteLine("# Gemipedia");
        outWriter.WriteLine("Gemipedia supports all of the languages that have a Wikipedia. The Gemipedia interface will be in English, and all article content, references, images, and featured content will be in the choosen language. You can select a language below, or use a specific language by providing a 2 letter ISO 3166 code");
        //force englist for this list
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("List of Wikipedias", "en")} List of available Wikipedias");
        outWriter.WriteLine("");
        outWriter.WriteLine($"Current Language: {UserOptions.LangaugeName}");

        foreach (var lang in LanguageUtils.CommonLanguages)
        {
            outWriter.WriteLine($"=> {RouteOptions.WelcomeUrl(lang)} Use {LanguageUtils.GetName(lang)}");
        }
        outWriter.WriteLine($"=> {RouteOptions.SetLanguageUrl()} Set specific language");

        RenderFooter(outWriter);
    }

    public static void SetLanguage(CgiWrapper cgi)
    {
        if (cgi.HasQuery)
        {
            //see if its valid
            if (LanguageUtils.IsValidCode(cgi.Query))
            {
                cgi.Redirect(RouteOptions.WelcomeUrl(cgi.Query));
                return;
            }
        }
        cgi.Input("Enter 2 letter ISO 3166 language code to use");
        return;
    }

    public static void Welcome(CgiWrapper cgi)
    {
        cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
        var outWriter = new CountingTextWriter(cgi.Writer);

        outWriter.WriteLine("# Gemipedia");
        outWriter.WriteLine("Welcome to Gemipedia: A Gemini frontend to Wikipedia, focused on providing a 1st class reading experience.");
        outWriter.WriteLine("");
        outWriter.WriteLine($"=> {RouteOptions.SelectLanguageUrl()} Using {UserOptions.LangaugeName} Wikipedia. Change Language?");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl()} Go to Article");
        outWriter.WriteLine($"=> {RouteOptions.SearchUrl()} Search for Articles containing a phrase");
        outWriter.WriteLine($"=> {RouteOptions.RandomArticleUrl()} 🎲 Random Article");
        outWriter.WriteLine("");

        outWriter.WriteLine("## Featured Content");
        if (UserOptions.WikipediaVersion == "en")
        {
            outWriter.WriteLine($"=> {RouteOptions.FeaturedContent()} Featured Article and 25 most popular articles (updated daily)");
        }
        else
        {
            outWriter.WriteLine($"=> {RouteOptions.FeaturedContent()} Featured Article and 25 most popular articles on {UserOptions.LangaugeName} Wikipedia (updated daily)");
        }

        outWriter.WriteLine("## Article Examples:");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("History of Apple Inc.")} History of Apple Inc.");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("Blue Poles")} Blue Poles");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("Gemini (protocol)")} Gemini (protocol)");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("Computer network")} Computer network");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("Interface Message Processor")} Interface Message Processor");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl("ALOHAnet")} ALOHAnet");
    }

    public static void ViewFeatured(CgiWrapper cgi)
    {
        cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
        var outWriter = new CountingTextWriter(cgi.Writer);

        outWriter.WriteLine($"# Gemipedia Featured Content {DateTime.Now.ToString("yyyy-MM-dd")}");
        outWriter.WriteLine("Compelling content pulled every day from the from page of Wikipedia");

        outWriter.WriteLine("## Daily Featured Article");

        var featured = client.GetFeaturedContent();

        if (featured.FeaturedArticle != null)
        {
            outWriter.WriteLine($"=> {RouteOptions.ArticleUrl(featured.FeaturedArticle.Title)} {featured.FeaturedArticle.Title}");
            if (!string.IsNullOrEmpty(featured.FeaturedArticle.ThumbnailUrl))
            {
                outWriter.WriteLine($"=> {RouteOptions.MediaProxyUrl(featured.FeaturedArticle.ThumbnailUrl)} Featured Image: {featured.FeaturedArticle.Title}");
            }
            outWriter.WriteLine($">{featured.FeaturedArticle.Excerpt}");
            outWriter.WriteLine();
        }
        else
        {
            outWriter.WriteLine("(Featured article was unavailable)");
        }

        outWriter.WriteLine("### 25 most viewed articles on Wikipedia today");

        if (featured.PopularArticles.Count > 0)
        {
            int counter = 0;
            foreach (var article in featured.PopularArticles)
            {
                counter++;
                outWriter.WriteLine($"=> {RouteOptions.ArticleUrl(article.Title)} {counter}. {article.Title}");
                if (!string.IsNullOrEmpty(article.ThumbnailUrl))
                {
                    outWriter.WriteLine($"=> {RouteOptions.MediaProxyUrl(article.ThumbnailUrl)} Featured Image: {article.Title}");
                }
                outWriter.WriteLine($">{article.SummaryText}");
                outWriter.WriteLine();
            }
        }
        else
        {
            outWriter.WriteLine("(Daily popular articles were unavailable)");
        }
        RenderFooter(outWriter, client.DownloadTimeMs);
    }

    public static void ViewGeo(CgiWrapper cgi)
    {
        GeohackParser geoparser = null;
        geoparser = new GeohackParser(cgi.Query);
        if (geoparser.IsValid)
        {
            cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
            var outWriter = new CountingTextWriter(cgi.Writer);

            var renderer = new GeoRenderer();
            renderer.RenderGeo(geoparser, outWriter);
            RenderFooter(outWriter);
            return;
        }

        cgi.BadRequest("Invalid geo information");
        return;
    }

    public static void ViewRandomArticle(CgiWrapper cgi)
    {
        string title = client.GetRandomArticleTitle();
        cgi.Redirect(RouteOptions.ArticleUrl(title));
    }

    public static void ViewArticle(CgiWrapper cgi)
    {
        if (!cgi.HasQuery)
        {
            cgi.Input("Article Name? (doesn't need to be exact)");
            return;
        }

        var outWriter = new CountingTextWriter(cgi.Writer);

        Article article = GetArticle(cgi);
        try
        {
            if (article != null)
            {
                if (RedirectParser.IsArticleRedirect(article.HtmlText))
                {
                    cgi.Redirect(RouteOptions.ArticleUrl(RedirectParser.GetRedirectTitle(article.HtmlText)));
                    return;
                }

                cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");

                ParsedPage parsedPage = converter.Convert(article.Title, article.HtmlText);
                RenderArticle(parsedPage, outWriter);
            }
            else
            {
                //redirect to search...
                cgi.Redirect(RouteOptions.SearchUrl(cgi.Query));
                return;
            }
        }
        catch (Exception ex)
        {
            outWriter.WriteLine("Boom! Hit Exception!");
            outWriter.WriteLine("```");
            outWriter.WriteLine(ex.Message);
            outWriter.WriteLine(ex.Source);
            outWriter.WriteLine(ex.StackTrace);
            outWriter.WriteLine("```");
        }
        RenderFooter(outWriter, client.DownloadTimeMs, converter.ConvertTimeMs);
    }

    public static void ViewImages(CgiWrapper cgi)
    {
        var outWriter = new CountingTextWriter(cgi.Writer);

        Article article = GetArticle(cgi);

        if (article != null)
        {
            if (RedirectParser.IsArticleRedirect(article.HtmlText))
            {
                cgi.Redirect(RouteOptions.ImageGalleryUrl(RedirectParser.GetRedirectTitle(article.HtmlText)));
                return;
            }

            cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
            ParsedPage page = converter.Convert(article.Title, article.HtmlText);
            var gallery = new GalleryRenderer();
            gallery.RenderGallery(page, outWriter);
        }
        else
        {
            cgi.Success();
            outWriter.WriteLine("We could not access that article");
        }
        RenderFooter(outWriter, client.DownloadTimeMs, converter.ConvertTimeMs);
    }

    public static void ViewOtherLanguages(CgiWrapper cgi)
    {
        var title = cgi.SantiziedQuery;
        var otherLangs = client.GetOtherLanguages(title);

        cgi.Success();
        var outWriter = new CountingTextWriter(cgi.Writer);

        outWriter.WriteLine($"# Other Languages");
        outWriter.WriteLine($"The article '{title}' is available in {otherLangs.Count} other languages");
        if (otherLangs.Count == 0)
        {
            outWriter.WriteLine("No languages found.");
            return;
        }
        else
        {
            foreach (var lang in otherLangs)
            {
                outWriter.WriteLine($"=> {RouteOptions.ArticleUrl(lang.Title, lang.LanguageCode)} {LanguageUtils.GetName(lang.LanguageCode)} - {lang.Title}");
            }
        }
        RenderFooter(outWriter, client.DownloadTimeMs);
    }

    public static void ViewRefs(CgiWrapper cgi)
    {
        var query = HttpUtility.ParseQueryString(cgi.RawQuery);
        var title = query["name"] ?? "";
        var section = Convert.ToInt32(query["section"] ?? "-1");

        Article article = GetArticle(title);
        var outWriter = new CountingTextWriter(cgi.Writer);

        if (article != null)
        {
            cgi.Success($"text/gemini;lang={UserOptions.WikipediaVersion}");
            ParsedPage parsedPage = converter.Convert(article.Title, article.HtmlText);
            var refs = new ReferencesRenderer();
            refs.RenderReferences(parsedPage, outWriter, section);
        }
        else
        {
            cgi.Success();
            outWriter.WriteLine("We could not access that article");
        }
        RenderFooter(outWriter, client.DownloadTimeMs, converter.ConvertTimeMs);
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

    static WikipediaApiClient client = new WikipediaApiClient(UserOptions.WikipediaVersion);
    static WikiHtmlConverter converter = new WikiHtmlConverter();

    private static Article GetArticle(CgiWrapper cgi)
        => GetArticle(cgi.SantiziedQuery);

    private static Article GetArticle(string title)
        => client.GetArticle(title);

    private static void RenderArticle(ParsedPage page, TextWriter output)
    {
        var renderer = new ArticleRenderer();
        renderer.RenderArticle(page, output);
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

    static void RenderFooter(CountingTextWriter outWriter, long? downloadTimeMs = null, long? convertTimeMs = null)
    {
        outWriter.WriteLine();
        outWriter.WriteLine("--");
        outWriter.WriteLine($"=> {RouteOptions.WelcomeUrl()} Gemipedia Home");
        outWriter.WriteLine($"=> {RouteOptions.ArticleUrl()} Go to Article");
        outWriter.WriteLine($"=> {RouteOptions.SelectLanguageUrl()} Using {UserOptions.LangaugeName} Wikipedia. Change Language?");
        outWriter.WriteLine("--");


        if (downloadTimeMs != null)
        {
            int outputSize = Convert.ToInt32(outWriter.ByteCount);
            outWriter.WriteLine($"Size: {ReadableFileSize(outputSize)}. {Savings(outputSize, client.DownloadSize)} smaller than original: {ReadableFileSize(client.DownloadSize)} 🤮");
        }

        if (downloadTimeMs != null || convertTimeMs != null)
        {
            if (downloadTimeMs != null)
            {
                outWriter.Write($"Fetched: {downloadTimeMs} ms. ");
            }
            if (convertTimeMs != null)
            {
                outWriter.Write($"Converted: {convertTimeMs} ms. ");
            }
            outWriter.WriteLine("🐇");
        }
        outWriter.WriteLine("=> mailto:acidus@gemi.dev Made with 📚 and ❤️ by Acidus");
        outWriter.WriteLine("All Wikipedia content is licensed under CC BY-SA 3.0");
    }


    private static string Savings(int newSize, int originalSize)
        => string.Format("{0:0.00}%", (1.0d - (Convert.ToDouble(newSize) / Convert.ToDouble(originalSize))) * 100.0d);

    private static string ReadableFileSize(double size, int unit = 0)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        while (size >= 1024)
        {
            size /= 1024;
            ++unit;
        }

        return string.Format("{0:0.0#} {1}", size, units[unit]);
    }
}