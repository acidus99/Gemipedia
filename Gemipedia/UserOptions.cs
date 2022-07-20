using System;
namespace Gemipedia
{
    public static class UserOptions
    {
        /// <summary>
        /// Set which version of Wikipedia we should use
        /// </summary>
        public static string WikipediaVersion { get; set; } = "en";

        //these will depend on the language

        public static string[] ExcludedSections
            => GetExclusedSections(WikipediaVersion);

        public static string[] ArticleLinkSections
            => GetArticleLinkSections(WikipediaVersion);

        static string [] GetExclusedSections(string language)
        {
            switch(language)
            {
                default:
                    return new string []{ "bibliography", "citations", "external_links", "notes", "references", "further_reading" };
            }
        }

        static string[] GetArticleLinkSections(string language)
        {
            switch (language)
            {
                default:
                    return new string[] { "see also" };
            }
        }
    }
}

