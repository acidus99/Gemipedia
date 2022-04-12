using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Parser.Tables;
using Gemipedia.Converter.Models;


namespace Gemipedia.Converter.Parser
{
    /// <summary>
    /// Converts the various image widgets
    /// </summary>
    public class ImageBlockParser
    {
        TextExtractor textExtractor = new TextExtractor();

        public SectionItem ConvertImageBlock(HtmlElement element)
        {
            //try and find the image URL
            var url = GetImageUrl(element);
            if(url == null)
            {
                return null;
            }

            url = EnsureHttps(url);
            textExtractor.ArticleLinks.Clear();
            var description = GetDescription(element);
            return new MediaItem
            {
                Caption = description,
                Url = CommonUtils.MediaProxyUrl(url),                
            };
        }

        private string GetImageUrl(HtmlElement element)
        {
            //try the srcset
            var url = GetImageFromSrcset(element.QuerySelector("img")?.GetAttribute("srcset") ?? "", "2x");
            if (url != null)
            {
                return url;
            }
            return element.QuerySelector("img")?.GetAttribute("src") ?? null;
        }

        private string EnsureHttps(string url)
            => (!url.StartsWith("https:")) ?
                "https:" + url :
                url;

        private string GetDescription(HtmlElement element)
        {
            //first see if there is a caption
            var captionTag = element.QuerySelector("div.thumbcaption");
            if(captionTag != null)
            {
                return textExtractor.GetText(element);
            }
            //fall back to the ALT text
            var text = GetImageAlt(element);
            return (text.Length > 0) ? text : "Article Image";
        }


        private string GetImageAlt(HtmlElement element)
            => element.QuerySelector("img")?.GetAttribute("alt") ?? "";

        private string GetImageFromSrcset(string srcset, string size)
        {
            if (srcset.Length > 0)
            {
                Regex parser = new Regex(@"(\S*[^,\s])(\s+([\d.]+)(x|w))?");

                return parser.Matches(srcset)
                    .Where(x => x.Success && x.Groups[2].Value.Trim() == size)
                    .Select(x => x.Groups[1].Value).FirstOrDefault() ?? null; ; ;
            }
            return null;
        }
    }
}
