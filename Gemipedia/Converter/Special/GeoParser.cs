using System;
using AngleSharp.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special;

public static class GeoParser
{
    public const string GeohackHostname = "geohack.toolforge.org";

    public static bool IsGeoLink(IElement anchor)
    {
        //only external links can be a link to geohack.
        //This fast-fails so we don't parse a bunch of relative, local, URLs
        if (!(anchor.GetAttribute("class")?.Contains("external") ?? false))
        {
            return false;
        }
        return IsGeohackUrl(anchor.GetAttribute("href"));
    }

    /// <summary>
    /// Is this url a valid link to the Wikipedia Geohack server?
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static bool IsGeohackUrl(string? url)
    {
        if (url == null)
        {
            return false;
        }

        try
        {
            Uri parsedUrl = new Uri(url);
            return (parsedUrl.IsAbsoluteUri && parsedUrl.Host == GeohackHostname);
        }
        catch (Exception)
        {
        }
        return false;
    }

    public static GeoItem ParseGeo(IElement anchor)
    {
        string url = anchor.GetAttribute("href");
        url = CommonUtils.EnsureHttps(url);

        GeohackParser geohack = new GeohackParser(url);
        if (geohack.IsValid)
        {
            return new GeoItem
            {
                Title = $"View Geographic Info: {geohack.GetPrettyName()} ({geohack.Coordinates})",
                Url = RouteOptions.GeoUrl(geohack.GeohackUrl)
            };
        }
        return null;
    }
}

