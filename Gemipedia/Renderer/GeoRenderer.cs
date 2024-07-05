using System.IO;
using Gemipedia.Converter.Special;

namespace Gemipedia.Renderer;

public class GeoRenderer
{
    TextWriter Writer;

    public void RenderGeo(GeohackParser geohack, TextWriter writer)
    {
        Writer = writer;

        Writer.WriteLine($"# Geographic Info for {geohack.GetPrettyName()}");
        Writer.WriteLine($"=> {RouteOptions.ArticleUrl(geohack.ArticleName)} Back to article");
        Writer.WriteLine();
        Writer.WriteLine($"Place: {geohack.GetPrettyName()}");
        if(!geohack.IsEarth)
        {
            Writer.WriteLine($"Globe: {geohack.Globe}");
        }
        if(geohack.HasTypeDescription)
        {
            Writer.WriteLine($"Type: {geohack.GetTypeDescription()}");
        }
        Writer.WriteLine($"Coordinates:");
        Writer.WriteLine($"* Latitude: {geohack.Latitude.ToString("#.####")}");
        Writer.WriteLine($"* Longitude: {geohack.Longitude.ToString("#.####")}");
        Writer.WriteLine();

        if (geohack.IsEarth)
        {
            Writer.WriteLine("## Mapping");
            Writer.WriteLine($"=> {OpenStreetMAps(geohack)} Open in OpenStreetMaps.org");
            Writer.WriteLine($"=> {AppleMapsUrl(geohack)} Open in Apple Maps app");
            Writer.WriteLine($"=> {GeoUrl(geohack)} Open in default Andriod Maps app (uses geo: URI)");
            Writer.WriteLine();
        }

        Writer.WriteLine("## Extras");
        Writer.WriteLine($"=> {geohack.GeohackUrl} Open in GeoHack Launcher");
        Writer.WriteLine($"=> {RouteOptions.LonLatUrl(geohack.Latitude, geohack.Longitude, geohack.ArticleName)} Search for nearby articles");
    }

    private string AppleMapsUrl(GeohackParser geohack)
        => $"https://maps.apple.com/?q={geohack.Latitude},{geohack.Longitude}&t=m";

    private string GeoUrl(GeohackParser geohack)
        => $"geo:{geohack.Latitude},{geohack.Longitude}?z=5";

    private string OpenStreetMAps(GeohackParser geohack)
        => $"https://www.openstreetmap.org/?mlat={geohack.Latitude}&mlon={geohack.Longitude}&zoom=15";
}