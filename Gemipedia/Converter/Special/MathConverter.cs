using AngleSharp.Html.Dom;

namespace Gemipedia.Converter.Special;

public static class MathConverter
{
    /// <summary>
    /// Attempts to convert an inline Math element into a linkable image
    /// Math formulas are in SVG, so link to our converter
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static string ConvertMath(HtmlElement element)
    {
        var img = element.QuerySelector("img");
        var url = img?.GetAttribute("src") ?? "";
        var caption = img?.GetAttribute("alt").Trim().Replace("\n", "") ?? "";

        if (url.Length > 0 && caption.Length > 0)
        {
            //not a media item, since it shouldn't be moved
            return $"=> {RouteOptions.MediaProxyUrl(MathSvgUrlAsPng(url))} Math Formula: {CleanLatex(caption)}";
        }
        return "";
    }

    //wikipedia has direct PNG versions of the SVG math images
    private static string MathSvgUrlAsPng(string url)
        => url.Replace("/svg/", "/png/");

    private static string CleanLatex(string latex)
        => latex.Replace(@"\displaystyle ", "");

}
