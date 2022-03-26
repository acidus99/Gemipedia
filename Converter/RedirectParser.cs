using System;
using System.Text.RegularExpressions;
namespace Gemipedia.Converter
{
    /// <summary>
    /// Handles redirects via Wikitext
    /// </summary>
    public static class RedirectParser
    {

        static Regex redirectTitle = new Regex("title=\"([^\\\"]+)", RegexOptions.Compiled);

        public static bool IsArticleRedirect(string html)
           => html.Contains("<div class=\"redirectMsg\">");

        public static string GetRedirectTitle(string html)
        {
            Match match = redirectTitle.Match(html);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "";
        }
    }
}
