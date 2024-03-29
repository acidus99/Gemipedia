﻿using System;
using System.Collections.Generic;
using System.Net;

using Gemipedia.API.Models;
using Gemipedia.Cache;

namespace Gemipedia.API
{

    public class WikipediaApiClient
    {
        static DiskCache Cache = new DiskCache();

        WebClient client;
        string Language;

        public WikipediaApiClient(string lang = "en")
        {
            client = new WebClient();
            Language = lang;
            client.Headers.Add(HttpRequestHeader.UserAgent, "GeminiProxy/0.1 (gemini://gemi.dev/; acidus@gemi.dev) gemini-proxy/0.1");
        }

        public List<ArticleSummary> GeoSearch(double lat, double lon)
        {
            var url = $"https://{Language}.wikipedia.org/w/api.php?action=query&format=json&list=geosearch&gscoord={lat}%7C{lon}&gsradius=5000&gslimit=100";
            return ResponseParser.ParseGeoSearch(FetchString(url));
        }

        //Gets the title of a random article
        public string GetRandomArticleTitle()
        {
            var url = $"https://{Language}.wikipedia.org/w/api.php?action=query&format=json&list=random&rnnamespace=0&rnlimit=1";
            //bypass the cache
            return ResponseParser.ParseRandomArticle(client.DownloadString(url));
        }

        /// <summary>
        /// Gets an article
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public Article GetArticle(string title)
        {
            var url = $"https://{Language}.wikipedia.org/w/api.php?action=parse&page={WebUtility.UrlEncode(title)}&prop=text&format=json";
            return ResponseParser.ParseArticleResponse(FetchString(url));
        }

        public FeaturedContent GetFeaturedContent()
        {
            //if you fetch the most popular content early in the day, there aren't any popular articles
            var url = $"https://{Language}.wikipedia.org/api/rest_v1/feed/featured/{DateTime.Now.ToString("yyyy/MM/dd")}";

            var featured = ResponseParser.ParseFeaturedContentResponse(FetchString(url));

            if(featured.PopularArticles.Count == 0)
            {
                //clear the cache
                Cache.Clear(url);

                var yesterday = DateTime.Now.Subtract(new TimeSpan(24, 0, 0));
                //fetch yesterdays most popular articles
                url = $"https://{Language}.wikipedia.org/api/rest_v1/feed/featured/{yesterday.ToString("yyyy/MM/dd")}";
                var oldFeatured = ResponseParser.ParseFeaturedContentResponse(FetchString(url));

                featured.PopularArticles = oldFeatured.PopularArticles;
            }

            return featured;
        }

        public List<ArticleSummary> GetOtherLanguages(string title)
        {
            //API wants this underscore encoded
            title = title.Replace(" ", "_");
            var url = $"https://{Language}.wikipedia.org/w/rest.php/v1/page/{WebUtility.UrlEncode(title)}/links/language";
            return ResponseParser.ParseOtherLanguagesResponse(FetchString(url));
        }

        /// <summary>
        /// Performance a search using the "rest.php/v1/search/page" endpoint
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<ArticleSummary> Search(string query)
        {
            var url = $"https://{Language}.wikipedia.org/w/rest.php/v1/search/page?q={WebUtility.UrlEncode(query)}&limit=25";
            return ResponseParser.ParseSearchResponse(FetchString(url));
        }

        //gets an image 
        public byte [] GetMedia(string url)
            => FetchBytes(url);

        //Downloads a string, if its not already cached
        private string FetchString(string url)
        {
            //first check the cache
            var contents = Cache.GetAsString(url);
            if(contents != null)
            {
                return contents;
            }
            //fetch it
            contents = client.DownloadString(url);
            //cache it
            Cache.Set(url, contents);
            return contents;
        }

        /// <summary>
        /// Fetchs the bytes for a URL. If it exists in the cache, it gets pulled
        /// otherwise a network request happens, and the results are cached
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private byte [] FetchBytes(string url)
        {
            //first check the cache
            var contents = Cache.GetAsBytes(url);
            if (contents != null)
            {
                return contents;
            }
            //fetch it
            contents = client.DownloadData(url);
            //cache it
            Cache.Set(url, contents);
            return contents;
        }
    }
}