using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Gemipedia.Converter.Models
{
    public abstract class SectionItem
    {
        public abstract string Render();
    }

    public class ContentItem : SectionItem, IArticleLinks
    {
        public ArticleLinkCollection ArticleLinks { get; set; }

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

    public class InfoboxItem : SectionItem
    {
        public List<SectionItem> Items = new List<SectionItem>();

        public override string Render()
        {
            StringBuilder sb = new StringBuilder();
            Items.ToList().ForEach(x => sb.Append(x.ToString()));
            return sb.ToString();
        }
    }

}
