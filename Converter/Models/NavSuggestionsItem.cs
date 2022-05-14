using System;
using System.Text;
namespace Gemipedia.Converter.Models
{
    public class NavSuggestionsItem : ContentItem
    {
        public NavSuggestionsItem(ITextContent textContent)
            : base(textContent) { }

        public override string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"({Content})");
            foreach (var linkTitle in Links.GetLinks())
            {
                sb.AppendLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
            }
            return sb.ToString();
        }
    }
}

