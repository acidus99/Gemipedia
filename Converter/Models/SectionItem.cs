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

    public class MediaItem : SectionItem, IArticleLinks
    {
        public string Url;
        public string Caption;
        public ArticleLinkCollection ArticleLinks { get; set; } = new ArticleLinkCollection();

        public override string Render()
            => $"=> {Url} {Caption}\n";
    }

    public class VideoItem : MediaItem, IArticleLinks
    {
        public string VideoUrl;
        public string VideoDescription;

        public override string Render()
            => $"=> {Url} Video Still: {Caption}\n=> {VideoUrl} Source Video: {VideoDescription}\n";
    }

    public class NavSuggestionsItem : ContentItem
    {
        public override string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"({Content})");
            foreach (var linkTitle in ArticleLinks.GetLinks())
            {
                sb.AppendLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
            }
            return sb.ToString();
        }
    }

    public class InfoboxItem : ContentItem
    {
        public string CustomTitle;

        public List<MediaItem> MediaItems = new List<MediaItem>();

        public override string Render()
        {

            bool renderText = !string.IsNullOrEmpty(Content.Trim());

            var title = string.IsNullOrEmpty(CustomTitle)
                ? "Quick Facts" :
                $"Quick Facts: {CustomTitle}";

            var sb = new StringBuilder();
            if (renderText)
            {
                sb.AppendLine($"### {title}");
            }
            foreach (var media in MediaItems)
            {
                sb.Append(media.Render());
            }
            if (renderText)
            {
                sb.Append(Content);
            }
            return sb.ToString();
        }
    }

}
