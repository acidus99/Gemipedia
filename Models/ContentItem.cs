using System;
namespace Gemipedia.Models
{
    public class ContentItem : SectionItem, ITextContent
    {
        public ArticleLinkCollection Links { get; set; }

        public string Content { get; set; }

        public ContentItem() { }

        public ContentItem(ITextContent textContent)
        {
            Content = textContent.Content;
            Links = textContent.Links;
        }

        public override string Render()
            => Content;
    }
}

