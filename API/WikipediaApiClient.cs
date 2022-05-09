using System;
using System.Collections.Generic;
using System.Net;

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

        public ParseResponse GetArticle(string title)
        {
            var url = $"https://en.wikipedia.org/w/api.php?action=parse&page={WebUtility.UrlEncode(title)}&prop=text&format=json";

            var json = FetchString(url);
            var resp = JObject.Parse(json);

            if(resp["error"] != null)
            {
                //error loading page!
                return null;
            }

            return new ParseResponse
            {
                Title = Cleanse(resp["parse"]["title"]),
                PageId = Convert.ToInt64(Cleanse(resp["parse"]["pageid"])),
                HtmlText = Cleanse(resp["parse"]["text"]["*"]),
            };
        }

        public List<SearchResult> Search(string query)
        {
            var url = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={WebUtility.UrlEncode(query)}&format=json";

            var json = FetchString(url);
            var resp = JObject.Parse(json);

            List<SearchResult> ret = new List<SearchResult>();

            foreach(JObject result in (resp["query"]["search"] as JArray))
            {
                ret.Add(new SearchResult
                {
                    Title = Cleanse(result["title"]),
                    PageId = Convert.ToInt64(Cleanse(result["pageid"])),
                    WordCount = Convert.ToInt32(Cleanse(result["wordcount"])),
                    Snippet = Cleanse(result["snippet"])
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
