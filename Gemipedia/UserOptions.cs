﻿using System;
using System.Globalization;
namespace Gemipedia
{
    public static class UserOptions
    {
        /// <summary>
        /// Set which version of Wikipedia we should use
        /// </summary>
        public static string WikipediaVersion { get; set; } = "en";

        public static string LangaugeName => GetLangaugeName(WikipediaVersion);

        public static string GetLangaugeName(string language)
        {
            try
            {
                var ci = new CultureInfo(language);
                return $"{ci.NativeName} ({ci.DisplayName})";
            }
            catch (Exception)
            {

            }
            return $"'{language}'";
        }

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

