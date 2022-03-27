using System;
using System.Linq;
using System.Collections.Generic;

namespace Gemipedia.Converter.Models
{
    public abstract class SectionItem
    {
        public abstract string Render();
    }

    public class ContentItem : SectionItem, ILinkedArticles
    {
        public List<String> LinkedArticles { get; set; } = new List<string>();

        public string Content { get; set; }

        public override string Render()
            => Content;
    }

    public class MediaItem : SectionItem
    {
        public string Url;
        public string Caption;

        public override string Render()
            => $"=> {Url} {Caption}\n";
    }

    public class NavSuggestionsItem : SectionItem
    {
        public List<NavSuggestion> Suggestions = new List<NavSuggestion>();

        public override string Render()
            => string.Join('\n', Suggestions
                .Select(x => $"=> {x.ArticleTitle} {x.Description}\n"));
    }

    public class NavSuggestion
    {
        public string ArticleTitle;
        public string Description;
    }

}
