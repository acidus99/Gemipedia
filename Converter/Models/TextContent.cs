using System;
namespace Gemipedia.Converter.Models
{
    public interface ITextContent : IArticleLinks
    {
        string Content { get; }
    }

    public class TextContent : ITextContent
    {
        public string Content { get; set; }
        public ArticleLinkCollection Links { get; set; }
    }
}

