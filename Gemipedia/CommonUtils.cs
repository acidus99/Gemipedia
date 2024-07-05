using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace Gemipedia;

public static class CommonUtils
{
    public static string PrepareTextContent(string s)
        => s.Trim().Replace("\n", "");


    /// <summary>
    /// Gets a properly formatted image URL from an IMG object 
    /// </summary>
    /// <param name="img"></param>
    /// <returns></returns>
    public static string GetImageUrl(IElement img)
    {
        //try srcset 2x
        var url = GetImageFromSrcset(img?.GetAttribute("srcset") ?? "", "2x");
        if (!string.IsNullOrEmpty(url))
        {
            return EnsureHttps(url);
        }
        //try srcset 1.5
        url = GetImageFromSrcset(img?.GetAttribute("srcset") ?? "", "1.5x");
        if (!string.IsNullOrEmpty(url))
        {
            return EnsureHttps(url);
        }
        return EnsureHttps(img.GetAttribute("src") ?? null);
    }

    public static string EnsureHttps(string url)
       => (url != null && !url.StartsWith("https:")) ?
           "https:" + url :
           url;

    private static string GetImageFromSrcset(string srcset, string size)
    {
        if (srcset.Length > 0)
        {
            Regex parser = new Regex(@"(\S*[^,\s])(\s+([\d.]+)(x|w))?");

            return parser.Matches(srcset)
                .Where(x => x.Success && x.Groups[2].Value.Trim() == size)
                .Select(x => x.Groups[1].Value).FirstOrDefault() ?? null;
        }
        return null;
    }


}
