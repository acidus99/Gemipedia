using System;

using Wcwidth;

namespace Gemipedia.Converter.Special.Tables
{
    public static class UnicodeString
    {
        //gets the actually fixed-width of a unicode string
        public static int GetWidth(string s)
        {
            int ret = 0;
            foreach (char c in s)
            {
                ret += UnicodeCalculator.GetWidth(c);
            }
            return ret;
        }
    }
}
