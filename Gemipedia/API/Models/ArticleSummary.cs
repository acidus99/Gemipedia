using System;
namespace Gemipedia.API.Models
{
    public class ArticleSummary
    {
        public string Title { get; set; }
        public long PageId { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }

        //distance in meters from where you were searching
        public int Distance { get; set; } = -1;

        /// <summary>
        /// Snippet of text where search term was found. Usually less helpful than description
        /// </summary>
        public string Excerpt { get; set; }

        public bool HasSummary
            =>!string.IsNullOrEmpty(SummaryText);

        public string SummaryText
            => !String.IsNullOrEmpty(Description) ? Description : Excerpt;
    }
}
