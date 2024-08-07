﻿using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;

namespace Gemipedia.Models;

public class ArticleLinkCollection
{
    Dictionary<string, ArticleLink> articles;

    public ArticleLinkCollection()
    {
        articles = new Dictionary<string, ArticleLink>();
    }

    public void Clear()
        => articles.Clear();

    public bool HasLinks
        => (Count > 0);

    public int Count
        => articles.Count;

    public void Add(ArticleLinkCollection collection)
    {
        foreach (string key in collection.articles.Keys)
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

    public void Add(IArticleLinks itemWithLinks)
        => Add(itemWithLinks.Links);

    public void Add(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        var key = title.ToLower();

        if (!articles.ContainsKey(key))
        {
            articles[key] = new ArticleLink(title);
        }
        else
        {
            articles[key].Occurences++;
        }
    }

    public void Add(IElement element)
    {
        if (ShouldUseLink(element))
        {
            Add(RemoveFragment(element.GetAttribute("title")));
        }
    }

    private string RemoveFragment(string title)
    {
        var index = title.IndexOf('#');
        return index > 0 ? title.Substring(0, index) : title;
    }



    public List<string> GetLinks()
        => articles.Keys.OrderBy(x => x).Select(x => articles[x].Title).ToList();

    public static bool ShouldUseLink(IElement element)
    {
        //wiki articles have a title attribute
        if (!element.HasAttribute("title"))
        {
            return false;
        }
        //links to pages that don't exist have a "new" class
        if (element.ClassList.Contains("new") || element.ClassList.Contains("internal"))
        {
            return false;
        }
        //hyperlinks should be relative, and start with "/wiki/"
        if (!(element.GetAttribute("href") ?? "").StartsWith("/wiki/"))
        {
            return false;
        }
        //should not be a link a special page
        var title = element.GetAttribute("title");
        if (title.StartsWith("Special:"))
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