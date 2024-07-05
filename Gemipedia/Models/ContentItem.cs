namespace Gemipedia.Models;

public class ContentItem : SectionItem, ITextContent
{
    public string Content { get; set; }

    public bool HasContent
        => (Content.Trim().Length > 0);

    public ArticleLinkCollection Links { get; set; }

    public ContentItem() { }

    public ContentItem(ITextContent textContent)
    {
        Content = textContent.Content;
        Links = textContent.Links;
    }
}