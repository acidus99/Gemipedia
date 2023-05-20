using System;
using System.Globalization;
namespace Gemipedia
{
	public static class LanguageUtils
	{
        public static readonly string [] CommonLanguages = new string[] { "simple", "ar", "bg", "ca", "ce", "cs", "da", "nl", "en", "eo", "fi", "fr", "de", "he", "hu", "id", "it", "ja", "ko", "ms", "zh", "no", "ga", "pl", "pt", "ro", "ru", "sr", "sh", "es", "sv", "tr", "uk", "vi" };

        public static string GetName(string langCode)
        {
            // Special logic for Simple English Wikipedia.
            if (IsSimpleEnglish(langCode))
            {
                return "Simple English";
            }

            try
            {
                var ci = new CultureInfo(langCode);
                return ci.NativeName == ci.DisplayName ?
                    ci.NativeName :
                    $"{ci.NativeName} ({ci.DisplayName})";
            }
            catch (Exception)
            {

            }
            return $"'{langCode}'";
        }

        public static bool IsValidCode(string langCode)
        {
            // Special logic for Simple English Wikipedia.
            if(IsSimpleEnglish(langCode))
            {
                return true;
            }

            try
            {
                var ci = new CultureInfo(langCode);
                return ci.DisplayName != langCode;
            }
            catch (Exception)
            { }
            return false;
        }

        private static bool IsSimpleEnglish(string langCode)
            => langCode.ToLower() == "simple";
    }
}

