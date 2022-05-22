using System;
using System.Collections.Generic;
using System.Text;

namespace Gemipedia.Models
{
    public class InfoboxItem : ContentItem
    {
        public string CustomTitle { get; set; }

        public List<MediaItem> MediaItems = new List<MediaItem>();

        public InfoboxItem(ITextContent textContent)
            :base(textContent) { }
    }
}

