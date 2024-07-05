using System;
using System.Collections.Generic;
using System.Net;
using CacheComms;
using Gemipedia.API.Models;

namespace Gemipedia.API;

/// <summary>
/// Wikipedia API client. Contacts the API and gets model objects back
/// </summary>
public class WikipediaApiClient
{
    HttpRequestor Requestor;
    string Language;

    public long DownloadTimeMs => Requestor.DownloadTimeMs;

    public int DownloadSize => Requestor.BodySize ?? 0;

    public WikipediaApiClient(string lang = "en")
    {
        Requestor = new HttpRequestor();
        Language = lang;
    }

    public List<ArticleSummary> GeoSearch(double lat, double lon)
    {
        var url = new Uri($"https://{Language}.wikipedia.org/w/api.php?action=query&format=json&list=geosearch&gscoord={lat}%7C{lon}&gsradius=5000&gslimit=100");
        string json = FetchString(url);
        return ResponseParser.ParseGeoSearch(json);
    }

    //Gets the title of a random article
    public string GetRandomArticleTitle()
    {
        var url = new Uri($"https://{Language}.wikipedia.org/w/api.php?action=query&format=json&list=random&rnnamespace=0&rnlimit=1");
        string json = FetchString(url);
        return ResponseParser.ParseRandomArticle(json);
    }

    /// <summary>
    /// Gets an article
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public Article GetArticle(string title)
    {
        var url = new Uri($"https://{Language}.wikipedia.org/w/api.php?action=parse&page={WebUtility.UrlEncode(title)}&prop=text&format=json");
        string json = FetchString(url);
        return ResponseParser.ParseArticleResponse(json);
    }

    public FeaturedContent GetFeaturedContent()
    {
        //if you fetch the most popular content early in the day, there aren't any popular articles
        var url = new Uri($"https://{Language}.wikipedia.org/api/rest_v1/feed/featured/{DateTime.Now.ToString("yyyy/MM/dd")}");
        //don't use the cace for this
        string json = FetchString(url);
        var featured = ResponseParser.ParseFeaturedContentResponse(json);

        if (featured.PopularArticles.Count == 0)
        {
            //fetch yesterdays
            var yesterday = DateTime.Now.Subtract(new TimeSpan(24, 0, 0));
            //fetch yesterdays most popular articles
            url = new Uri($"https://{Language}.wikipedia.org/api/rest_v1/feed/featured/{yesterday.ToString("yyyy/MM/dd")}");
            var oldFeatured = ResponseParser.ParseFeaturedContentResponse(FetchString(url));
            featured.PopularArticles = oldFeatured.PopularArticles;
        }

        return featured;
    }

    public List<ArticleSummary> GetOtherLanguages(string title)
    {
        //API wants whitespace encoded as underscores
        title = title.Replace(" ", "_");
        var url = new Uri($"https://{Language}.wikipedia.org/w/rest.php/v1/page/{WebUtility.UrlEncode(title)}/links/language");
        string json = FetchString(url);
        return ResponseParser.ParseOtherLanguagesResponse(json);
    }

    /// <summary>
    /// Performance a search using the "rest.php/v1/search/page" endpoint
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public List<ArticleSummary> Search(string query)
    {
        var url = new Uri($"https://{Language}.wikipedia.org/w/rest.php/v1/search/page?q={WebUtility.UrlEncode(query)}&limit=25");
        string json = FetchString(url);
        return ResponseParser.ParseSearchResponse(json);
    }

    //gets an image 
    public byte[] GetMedia(string url)
        => FetchBytes(url);

    //Downloads a string, if its not already cached
    private string FetchString(Uri url, bool useCache = true)
    {
        var result = Requestor.GetAsString(url, useCache);
        if (!result)
        {
            return "";
        }
        return Requestor.BodyText;
    }

    /// <summary>
    /// Fetchs the bytes for a URL. If it exists in the cache, it gets pulled
    /// otherwise a network request happens, and the results are cached
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private byte[] FetchBytes(string url, bool useCache = true)
    {
        var result = Requestor.GetAsBytes(new Uri(url), useCache);
        if (!result)
        {
            return null;
        }
        return Requestor.BodyBytes;
    }
}