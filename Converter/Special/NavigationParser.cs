using System;
using AngleSharp.Html.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special
{
    /// <summary>
    /// parses navigation notes 
    /// </summary>
    public class NavigationParser
    {
        /// <summary>
        /// Convert a navigation note in a section
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static NavSuggestionsItem ConvertNavigationNote(HtmlElement element)
        {
            var textExtractor = new TextExtractor
            {
                ShouldCollapseNewlines = true
            };
            textExtractor.Extract(element);
            return new NavSuggestionsItem(textExtractor);
        }
    }
}
