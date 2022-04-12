using System;
using System.Linq;
using System.Collections.Generic;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Gemipedia.Converter.Models
{
    public class ArticleLinkCollection
    {
        Dictionary<string, ArticleLink> articles;

        public ArticleLinkCollection()
        {
            articles = new Dictionary<string, ArticleLink>();
        }

        public bool HasLinks
            => (articles.Count > 0);

        public void Add(string title)
        {
            if(title.Length == 0)
            {
                return;
            }

            var key = title.ToLower();

            if(!articles.ContainsKey(key))
            {
                articles[key] = new ArticleLink(title);
            } else
            {
                articles[key].Occurences++;
            }
        }

        public void Add(HtmlElement element)
        {
            if (ShouldUseLink(element))
            {
                Add(element.GetAttribute("title"));
            }
        }

        public void MergeCollection(ArticleLinkCollection collection)
        {
            foreach(string key in collection.articles.Keys)
            {
                if (!articles.ContainsKey(key))
                {
                    articles[key] = collection.articles[key];
                }
                else
                {
                    articles[key].Occurences++;
                }
            }
        }

        public List<string> GetLinks()
            => articles.Keys.OrderBy(x => x).Select(x => articles[x].Title).ToList();

        public static bool ShouldUseLink(IElement element)
            => element.HasAttribute("title") &&
            //ignore links to special pages!
            !element.GetAttribute("title").StartsWith("Special:") &&
            //links to pages that don't exist have a "new" class
            !element.ClassList.Contains("new");
    }
}
