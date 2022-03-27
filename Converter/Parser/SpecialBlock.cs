using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;
namespace Gemipedia.Converter.Parser
{
    public static class SpecialBlock
    {
        public static string ConvertMath(HtmlElement element)
        {
            var img = element.QuerySelector("img");
            var url = img?.GetAttribute("src") ?? "";
            var caption = img?.GetAttribute("alt") ?? "";

            if (url.Length > 0 && caption.Length > 0)
            {
                //not a media item, since it shouldn't be moved
                return $"=> {CommonUtils.RewriteMediaUrl(url)} Math: {CommonUtils.PrepareTextContent(caption)}\n";
            }
            return "";
        }
    }
}
