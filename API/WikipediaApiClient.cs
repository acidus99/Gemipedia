using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;
using Gemipedia.API.Models;

using Gemipedia.Cache;

namespace Gemipedia.API
{

    public class WikipediaApiClient
    {
        static DiskCache Cache = new DiskCache();

        WebClient client;

        public WikipediaApiClient()
        {
            client = new WebClient();
            client.Headers.Add(HttpRequestHeader.UserAgent, "GeminiProxy/0.1 (gemini://gemi.dev/; acidus@gemi.dev) gemini-proxy/0.1");
        }

        public Article GetArticle(string title)
        {
            var url = $"https://en.wikipedia.org/w/api.php?action=parse&page={WebUtility.UrlEncode(title)}&prop=text&format=json";

            var json = FetchString(url);
            var resp = JObject.Parse(json);

            if(resp["error"] != null)
            {
                //error loading page!
                return null;
            }

            return new Article
            {
                Title = Cleanse(resp["parse"]["title"]),
                PageId = Convert.ToInt64(Cleanse(resp["parse"]["pageid"])),
                HtmlText = Cleanse(resp["parse"]["text"]["*"]),
            };
        }

        private string GetThumbnailUrl(JObject thumb)
        {
            //result["thumbnail"]?["url"]? doesn't seem to work
            if (thumb != null)
            {
                var url = thumb["url"]?.ToString() ?? "";
                if (url.Length > 0)
                {
                    return CommonUtils.EnsureHttps(url);
                }
            }
            return "";
        }

        private string StripHtml(string s)
            => WebUtility.HtmlDecode(Regex.Replace(s, @"<[^>]*>", "")) + "...";

        public string GetTodayFeed()
        {
            var url = $"https://en.wikipedia.org/api/rest_v1/feed/featured/{DateTime.Now.ToString("yyyy/mm/dd")}";
            var json = FetchString(url);
            var resp = JObject.Parse(json);

            return "";
        }


        public List<SearchResult> SearchBetter(string query)
        {
            var url = $"https://en.wikipedia.org/w/rest.php/v1/search/page?q={WebUtility.UrlEncode(query)}&limit=25";

            var json = FetchString(url);
            var resp = JObject.Parse(json);

            List<SearchResult> ret = new List<SearchResult>();

            foreach (JObject result in (resp["pages"] as JArray))
            {
                ret.Add(new SearchResult
                {
                    Title = Cleanse(result["title"]),
                    Excerpt = StripHtml(Cleanse(result["excerpt"])),
                    Description = Cleanse(result["description"]),
                    ThumbnailUrl = GetThumbnailUrl(result["thumbnail"] as JObject)
                });
            }
            return ret;
        }


        private string Cleanse(JToken token)
            => token?.ToString() ?? "";

        public byte [] GetMedia(string fullUrl)
            => FetchBytes(fullUrl);

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
