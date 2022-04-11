using System;
using System.Linq;
using System.Collections.Generic;

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

        public void AddArticle(string title)
        {
            var key = title.ToLower();

            if(!articles.ContainsKey(key))
            {
                articles[key] = new ArticleLink(title);
            } else
            {
                articles[key].Occurences++;
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

    }
}
