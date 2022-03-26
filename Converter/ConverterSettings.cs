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
        /// URL to use to proxy media. actually media path passed via query string
        /// </summary>
        public string MediaProxyUrl { get; set; }

        /// <summary>
        /// Should we convert images/media?
        /// </summary>
        public bool ShouldConvertMedia { get; set; }

    }
}
