using System;
namespace Gemipedia.API.Models
{
    public class ParseResponse
    {
        public string Title { get; set; }
        public long PageId{ get; set; }
        public string HtmlText { get; set; }
    }
}
