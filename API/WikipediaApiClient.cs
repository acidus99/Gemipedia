using System;
using System.Collections.Generic;
using System.Net;

using Newtonsoft.Json.Linq;
using Gemipedia.API.Models;

namespace Gemipedia.API
{

    public class WikipediaApiClient
    {
        WebClient client;

        public WikipediaApiClient()
        {
            client = new WebClient();
            client.Headers.Add(HttpRequestHeader.UserAgent, "GeminiProxy/0.1 (gemini://gemi.dev/; acidus@gemi.dev) gemini-proxy/0.1");
        }

        public ParseResponse GetArticle(string title)
        {
            var url = $"https://en.wikipedia.org/w/api.php?action=parse&page={WebUtility.UrlEncode(title)}&prop=text&format=json";

            var json = client.DownloadString(url);
                
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

            var json = client.DownloadString(url);
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

        public byte [] FetchMedia(string fullUrl)
        {
            return client.DownloadData(fullUrl);
        }
    }
}
