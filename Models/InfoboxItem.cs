using System;
using System.Collections.Generic;
using System.Text;

namespace Gemipedia.Models
{
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

