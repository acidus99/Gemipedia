using System;
namespace Gemipedia.Converter.Models
{
    public class MediaItem : SectionItem, IArticleLinks
    {
        public string Url { get; set; }
        public string Caption { get; set; }
        public ArticleLinkCollection Links { get; set; } = new ArticleLinkCollection();

        public override string Render()
            => $"=> {Url} {Caption}\n";
    }
}

