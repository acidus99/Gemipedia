using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
namespace Gemipedia.Converter.Special
{
	/// <summary>
    /// Parses the URLs used by Geohack.toolforge.org
    /// </summary>
	public class GeohackParser
	{
		public string ArticleName { get; private set; }

		public bool IsEarth
			=> (Globe.ToLower() == "earth");

		public string Globe { get; private set; }

		public string GeohackUrl { get; private set; }

		public string Language { get; private set; }

		public double Latitude { get; private set; }

		public double Longitude { get; private set; }

		public string Title { get; private set; }

		public string Type { get; private set; }

		public string Coordinates { get; private set; }

		public string GetPrettyName()
			=> Title.Length > 0 ? Title : ArticleName;

		public bool HasTypeDescription
			=> GetTypeDescription().Length > 0;

		public string GetTypeDescription()
        {
			switch(Type)
            {
				case "airport":
				case "city":
				case "country":
				case "event":
				case "forest":
				case "glacier":
				case "landmark":
				case "montain":
				case "river":
				case "satellite":
				case "state":
					return Type.Substring(0, 1).ToUpper() + Type.Substring(1);

				case "edu":
					return "Educational Institute";

				case "railwaystation":
					return "Railway Station";

				case "adm1st":
				case "adm2nd":
				case "adm3rd":
					return "Municipality";

				case "waterbody":
					return "Body of water";

				default:
					return "";

			}
        }

		Regex DegreeMinuteSecondDirection = new Regex(@"([\d\.]+)_+(?:([\d\.]+)_)?(?:([\d\.]+)_+)?([NS])_([\d\.]+)_+(?:([\d\.]+)_+)?(?:([\d\.]+)_)?([EW])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		Regex DegreeDirection = new Regex(@"([\-\.\d]+)_([NS])_([\-\.\d]+)_([EW])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		NameValueCollection QueryString;

		string ParamString => QueryString["params"];

		public GeohackParser(string geohackUrl)
		{
			Uri url = new Uri(geohackUrl);
			if(url.Host != "geohack.toolforge.org")
            {
				throw new ArgumentException("Not a Geohack url");
            }

			GeohackUrl = geohackUrl;

			QueryString = HttpUtility.ParseQueryString(url.Query);

			ParseLatLon();
			ArticleName = ParseArticleName();
			Globe = ExtractParam("globe") ?? "earth";
			Language = QueryString["language"] ?? "en";
			Title = QueryString["title"] ?? "";
			Type = ExtractParam("type");
		}

		private void ParseLatLon()
        {
			if (DegreeMinuteSecondDirection.IsMatch(ParamString))
			{
				ParseDMSD(ParamString);
			}
			else if (DegreeDirection.IsMatch(ParamString))
			{
				ParseDD(ParamString);
			}
			else
			{
				throw new ApplicationException("Unknown lat/lon format");
			}
        }

		private string ParseArticleName()
			=> QueryString["pagename"]?.Replace("_", " ") ?? "";

		private double NormalizeDMS(Group g)
		{
			var val = g.ToString();
			return val.Length > 0 ? Convert.ToDouble(val) : 0d;
		}

		private void ParseDMSD(string dms)
        {
			var match = DegreeMinuteSecondDirection.Match(dms);

			//DD = d + (min/60) + (sec/3600)
			Latitude = NormalizeDMS(match.Groups[1]) +
						NormalizeDMS(match.Groups[2]) / 60d +
						NormalizeDMS(match.Groups[3]) / 3600d;

			if(match.Groups[4].ToString().ToLower() == "s")
            {
				Latitude *= -1;
            }

			Longitude = NormalizeDMS(match.Groups[5]) +
						NormalizeDMS(match.Groups[6]) / 60d +
						NormalizeDMS(match.Groups[7]) / 3600d;

			if (match.Groups[8].ToString().ToLower() == "w")
			{
				Longitude *= -1;
			}

			Coordinates = string.Format("{0}{1}{2}{3} {4}{5}{6}{7}",
				FormatGroup(match.Groups[1], "°"),
				FormatGroup(match.Groups[2], "′"),
				FormatGroup(match.Groups[3], "″"),
				FormatGroup(match.Groups[4]),
				FormatGroup(match.Groups[5], "°"),
				FormatGroup(match.Groups[6], "′"),
				FormatGroup(match.Groups[7], "″"),
				FormatGroup(match.Groups[8]));
		}

		private string FormatGroup(Group g, string symbol="")
        {
			var val = g.ToString();
			return val.Length > 0 ? $"{val}{symbol}" : "";
        }

		private void ParseDD(string dd)
		{
			var match = DegreeDirection.Match(dd);

			//DD = d + (min/60) + (sec/3600)
			Latitude = Convert.ToDouble(match.Groups[1].ToString());

			if (match.Groups[2].ToString().ToLower() == "s")
			{
				Latitude *= -1;
			}

			Longitude = Convert.ToDouble(match.Groups[3].ToString());
			if (match.Groups[4].ToString().ToLower() == "w")
			{
				Longitude *= -1;
			}
			Coordinates = string.Format("{0}°{1} {2}°{3}",
				match.Groups[1], match.Groups[2],
				match.Groups[3], match.Groups[4]);
		}

		private string ExtractParam(string paramName)
        {
			var match = Regex.Match(ParamString, @$"_?{paramName}\:([a-zA-Z0-9]+)_?");
			if(match.Success && match.Groups.Count > 1)
            {
				return match.Groups[1].ToString();
            }
			return null;
        }

	}
}