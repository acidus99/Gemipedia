using System;
namespace Gemipedia.Models
{
    public interface ITextContent : IArticleLinks
    {
        string Content { get; }
    }
}

