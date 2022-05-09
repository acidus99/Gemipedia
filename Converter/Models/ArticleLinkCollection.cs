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
            => (Count > 0);

        public int Count
            => articles.Count;

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

        public void Add(IElement element)
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
        {
            //wiki articles have a title attribute
            if(!element.HasAttribute("title"))
            {
                return false;
            }
            //links to pages that don't exist have a "new" class
            if (element.ClassList.Contains("new") || element.ClassList.Contains("internal"))
            {
                return false;
            }
            //hyperlinks should be relative, and start with "/wiki/"
            if( !(element.GetAttribute("href") ?? "").StartsWith("/wiki/"))
            {
                return false;
            }
            //should not be a link a special page
            var title = element.GetAttribute("title");
            if(title.StartsWith("Special:"))
            {
                return false;
            }
            if (title.StartsWith("Help:"))
            {
                return false;
            }

            return true;
        }

    }
}
