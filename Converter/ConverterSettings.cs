using System;
namespace Gemipedia.Converter
{
    public class ConverterSettings
    {
        /// <summary>
        /// URL to use to view an artical. Actual artical passed via query string
        /// </summary>
        public string ArticleUrl { get; set; }

        /// <summary>
        /// Section titles we should exclude from the converted output
        /// </summary>
        public string[] ExcludedSections { get; set; }

        /// <summary>
        /// Section titles that are just lists of links to other articles.
        /// We don't want to duplicate our output in the index by display these again
        /// </summary>
        public string[] ArticleLinkSections { get; set; }

        /// <summary>
        /// URL to use to proxy media. actual media path passed via query string
        /// </summary>
        public string MediaProxyUrl { get; set; }

        public string ImageGallerUrl { get; set; }

        public string ReferencesUrl { get; set; }

        /// <summary>
        /// Should we convert images/media?
        /// </summary>
        public bool ShouldConvertMedia { get; set; } = true;

    }
}
