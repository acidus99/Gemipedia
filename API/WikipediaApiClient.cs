using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;
using WikiProxy.API.Models;

namespace WikiProxy.API
{
    /// <summary>
    /// Gets a locale via a free IP 2 location service
    /// </summary>
    public class WikipediaApiClient
    {
        WebClient client;

        static Regex redirectTitle = new Regex("title=\"([^\\\"]+)");

        public WikipediaApiClient()
        {
            client = new WebClient();
            client.Headers.Add(HttpRequestHeader.UserAgent, "GeminiProxy/0.1 (gemini://gemi.dev/; acidus@gemi.dev) gemini-proxy/0.1");
        }

        public ParseResponse GetArticle(string title)
        {
            int requests = 0;
            do
            {
                requests++;

                var url = $"https://en.wikipedia.org/w/api.php?action=parse&page={WebUtility.UrlEncode(title)}&prop=text&format=json";

                var json = client.DownloadString(url);
                var resp = JObject.Parse(json);

                var model = new ParseResponse
                {
                    Title = Cleanse(resp["parse"]["title"]),
                    PageId = Convert.ToInt64(Cleanse(resp["parse"]["pageid"])),
                    HtmlText = Cleanse(resp["parse"]["text"]["*"]),
                };

                if (!IsArticleRedirect(model.HtmlText))
                {
                    return model;
                }
                title = GetRedirectTitle(model.HtmlText);
            } while (requests < 4 && title != "");

            return null;
        }

        private bool IsArticleRedirect(string html)
            => html.Contains("<div class=\"redirectMsg\">");

        private string GetRedirectTitle(string html)
        {
            Match match = redirectTitle.Match(html);
            if(match.Success)
            {
                return match.Groups[1].Value;
            }
            return "";
        }

        public ParseResponse GetArticle(long pageID)
        {
            var url = $"https://en.wikipedia.org/w/api.php?action=parse&pageid={pageID}&prop=text&format=json";

            var json = client.DownloadString(url);
            var resp = JObject.Parse(json);
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
