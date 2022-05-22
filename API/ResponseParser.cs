using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using Gemipedia.API.Models;

namespace Gemipedia.API
{
	public static class ResponseParser
	{

		public static Article ParseArticleResponse(JObject response)
        {
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

        public static List<SearchResult> ParseSearchResponse(JObject response)
        {
            List<SearchResult> ret = new List<SearchResult>();

            foreach (JObject result in (response["pages"] as JArray))
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

        private static string GetThumbnailUrl(JObject thumb)
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

        private static string Cleanse(JToken token)
            => token?.ToString() ?? "";


        private static string StripHtml(string s)
            => WebUtility.HtmlDecode(Regex.Replace(s, @"<[^>]*>", "")) + "...";

    }
}

