using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Gemipedia.Models;
using Gemipedia.Converter.Special;

namespace Gemipedia.Renderer
{
    public class GeoRenderer
    {
        TextWriter Writer;

        public void RenderGeo(GeohackParser geohack, TextWriter writer)
        {
            Writer = writer;

            Writer.WriteLine($"# Geographic Info for {geohack.GetPrettyName()}");
            Writer.WriteLine($"=> {CommonUtils.ArticleUrl(geohack.ArticleName)} Back to article");
            Writer.WriteLine();
            Writer.WriteLine($"Place: {geohack.GetPrettyName()}");
            Writer.WriteLine($"Latitude: {geohack.Latitude}");
            Writer.WriteLine($"Longitude: {geohack.Longitude}");


            Writer.WriteLine($"=> {AppleMapsUrl(geohack)} Open in iOS Maps app");
            Writer.WriteLine($"=> {GeoUrl(geohack)} Open in default Andriod Maps app");
            Writer.WriteLine();
        }

        private string AppleMapsUrl(GeohackParser geohack)
            => $"https://maps.apple.com/?ll={geohack.Latitude},{geohack.Longitude}";

        private string GeoUrl(GeohackParser geohack)
            => $"geo:{geohack.Latitude},{geohack.Longitude}";
    }
}
