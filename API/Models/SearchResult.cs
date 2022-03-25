using System;
namespace WikiProxy.API.Models
{
    public class SearchResult
    {
        public string Title { get; set; }
        public long PageId { get; set; }
        public int WordCount { get; set; }
        public string Snippet { get; set; }
    }
}
