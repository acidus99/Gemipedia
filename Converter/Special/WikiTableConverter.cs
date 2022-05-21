using AngleSharp.Html.Dom;
using Gemipedia.Converter.Special.Tables;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special
{
    public static class WikiTableConverter
    {
        /// <summary>
        /// Convert a data table
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static SectionItem ConvertWikiTable(HtmlElement element)
        {

            //do we have a timeline?
            var media = MediaParser.ConvertTimelineInTable(element);
            if (media != null)
            {
                return media;
            }

            TableParser tableParser = new TableParser();
            var table = tableParser.ParseTable(element);

            var contents = TableRenderer.RenderTable(table);

            return new ContentItem
            {
                Content = contents,
                Links = tableParser.Links
            };
        }
    }
}
