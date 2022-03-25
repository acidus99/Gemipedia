using System;
namespace WikiProxy.API.Models
{
    public class ParseResponse
    {
        public string Title { get; set; }
        public long PageId{ get; set; }
        public string HtmlText { get; set; }
    }
}
