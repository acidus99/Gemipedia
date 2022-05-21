using System;
namespace Gemipedia.API.Models
{
    public class SearchResult
    {
        public string Title { get; set; }
        public long PageId { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Snippet of text where search term was found. Usually less helpful than description
        /// </summary>
        public string Excerpt { get; set; }

        public string SummaryText
            => !String.IsNullOrEmpty(Description) ? Description : Excerpt;

    }
}
