using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Gemipedia.API.Models;
using Newtonsoft.Json.Linq;

namespace Gemipedia.API;

/// <summary>
/// Parses the JSON responses of the Wikipedia API into model objects
/// </summary>
public static class ResponseParser
{
    public static Article ParseArticleResponse(string json)
    {
        var response = ParseJson(json);

        if (response["error"] != null)
        {
            //error loading page!
            return null;
        }

        return new Article
        {
            Title = Cleanse(response["parse"]["title"]),
            PageId = Convert.ToInt64(Cleanse(response["parse"]["pageid"])),
            HtmlText = Cleanse(response["parse"]["text"]["*"]),
        };
    }

    public static List<ArticleSummary> ParseGeoSearch(string json)
    {
        var response = ParseJson(json);
        List<ArticleSummary> ret = new List<ArticleSummary>();

        if (response["query"] != null && response["query"]["geosearch"] != null)
        {
            //skip the first since that's the article we are on

            foreach (JObject result in (response["query"]["geosearch"] as JArray).Skip(1))
            {
                ret.Add(new ArticleSummary
                {
                    Title = Cleanse(result["title"]),
                    Distance = (int)Math.Round(Convert.ToDouble(result["dist"]?.ToString() ?? "0"))
                });
            }
        }

        return ret;
    }

    public static List<ArticleSummary> ParseSearchResponse(string json)
    {
        var response = ParseJson(json);
        List<ArticleSummary> ret = new List<ArticleSummary>();
        foreach (JObject result in (response["pages"] as JArray))
        {
            ret.Add(new ArticleSummary
            {
                Title = StripNewlines(Cleanse(result["title"])),
                Excerpt = StripNewlines(StripHtml(Cleanse(result["excerpt"]))),
                Description = StripNewlines(Cleanse(result["description"])),
                ThumbnailUrl = GetThumbnailUrl(result["thumbnail"] as JObject)
            });
        }
        return ret;
    }

    public static List<ArticleSummary> ParseOtherLanguagesResponse(string json)
    {
        var response = JArray.Parse(json);
        List<ArticleSummary> ret = new List<ArticleSummary>();
        foreach (JObject result in response)
        {
            ret.Add(new ArticleSummary
            {
                Title = Cleanse(result["title"]),
                LanguageCode = Cleanse(result["code"])
            });
        }
        return ret;
    }

    public static FeaturedContent ParseFeaturedContentResponse(string json)
    {
        var response = ParseJson(json);
        return new FeaturedContent
        {
            FeaturedArticle = ParseArticleSummary(response["tfa"] as JObject),
            PopularArticles = ParsePopularArticles(response["mostread"] as JObject)
        };
    }

    public static string ParseRandomArticle(string json)
    {
        var response = ParseJson(json);
        return response["query"]["random"][0]["title"].Value<string>();
    }

    private static List<ArticleSummary> ParsePopularArticles(JObject articles)
    {
        List<ArticleSummary> ret = new List<ArticleSummary>();

        if (articles != null)
        {
            foreach (JObject article in (articles["articles"] as JArray).Take(25))
            {
                ret.Add(ParseArticleSummary(article));
            }
        }
        return ret;
    }

    private static ArticleSummary ParseArticleSummary(JObject summary)
        => (summary != null) ?
            new ArticleSummary
            {
                Title = StripNewlines(Cleanse(summary["normalizedtitle"])),
                Description = StripNewlines(Cleanse(summary["description"])),
                //already text formatted!
                Excerpt = StripNewlines(Cleanse(summary["extract"])),
                ThumbnailUrl = GetThumbnailUrl(summary["thumbnail"] as JObject)
            } : null;

    private static string GetThumbnailUrl(JObject thumb)
    {
        //result["thumbnail"]?["url"]? doesn't seem to work
        if (thumb != null)
        {
            var url = thumb["url"]?.ToString() ??
                        thumb["source"]?.ToString() ?? "";
            if (url.Length > 0)
            {
                return CommonUtils.EnsureHttps(url);
            }
        }

        return "";
    }

    private static string StripNewlines(string s)
        => s.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();

    private static string Cleanse(JToken token)
        => token?.ToString() ?? "";

    private static JObject ParseJson(string json)
        => JObject.Parse(json);

    private static string StripHtml(string s)
        => WebUtility.HtmlDecode(Regex.Replace(s, @"<[^>]*>", "")) + "...";
}

