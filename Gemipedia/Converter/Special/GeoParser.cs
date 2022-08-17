using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using AngleSharp.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special
{
	public static class GeoParser
	{
        public static bool IsGeoLink(IElement anchor)
            => ((anchor.GetAttribute("class")?.Contains("external") ?? false) &&
                (anchor.GetAttribute("href")?.StartsWith("//geohack.toolforge.org/") ?? false));

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
}

