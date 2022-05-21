using System;
using System.Text;
namespace Gemipedia.Models
{
    public class NavSuggestionsItem : ContentItem
    {
        public NavSuggestionsItem(ITextContent textContent)
            : base(textContent) { }

        public override string Render()
        {
            var sb = new StringBuilder();

            var links = Links.GetLinks();
            if (links.Count == 1)
            {
                sb.AppendLine($"=> {CommonUtils.ArticleUrl(links[0])} {Content}");
            }
            else
            {
                sb.AppendLine($"({Content})");
                foreach (var linkTitle in links)
                {
                    sb.AppendLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
                }
            }
            return sb.ToString();
        }
    }
}

