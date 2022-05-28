using System;
using System.Text.RegularExpressions;
using System.Web;
namespace Gemipedia.Converter.Special
{
	public class GeohackParser
	{
		public string ArticleName { get; private set; }

		public string GeohackUrl { get; private set; }

		public string Language { get; private set; }

		public double Latitude { get; private set; }

		public double Longitude { get; private set; }

		public string Title { get; private set; }

		public string GetPrettyName()
			=> Title.Length > 0 ? Title : ArticleName;

		Regex DegreeMinuteSecondDirection = new Regex(@"(\d+)_(\d+)_(\d+)_([NS])_(\d+)_(\d+)_(\d+)_([EW])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		Regex DegreeDirection = new Regex(@"([\-\.\d]+)_([NS])_([\-\.\d]+)_([EW])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public GeohackParser(string geohackUrl)
		{
			Uri url = new Uri(geohackUrl);
			if(url.Host != "geohack.toolforge.org")
            {
				throw new ArgumentException("Not a Geohack url");
            }

			GeohackUrl = geohackUrl;

			var query = HttpUtility.ParseQueryString(url.Query);
			ParseLonLot(query["params"]);

			ArticleName = query["pagename"]?.Replace("_", " ") ?? "";
			Title = query["title"] ?? "";
			Language = query["language"] ?? "en";
		}

		private void ParseLonLot(string param)
        {
			if (DegreeMinuteSecondDirection.IsMatch(param))
			{
				ParseDMSD(param);
			}
			else if (DegreeDirection.IsMatch(param))
			{
				ParseDD(param);
			}
			else
			{
				throw new ApplicationException("Unknown lon/lat format");
			}
        }

		private void ParseDMSD(string dms)
        {
			var match = DegreeMinuteSecondDirection.Match(dms);

			//DD = d + (min/60) + (sec/3600)
			Latitude = Convert.ToDouble(match.Groups[1].ToString()) +
						Convert.ToDouble(match.Groups[2].ToString()) / 60d +
						Convert.ToDouble(match.Groups[3].ToString()) / 3600d;

			if(match.Groups[4].ToString().ToLower() == "s")
            {
				Latitude *= -1;
            }

			Longitude = Convert.ToDouble(match.Groups[5].ToString()) +
						Convert.ToDouble(match.Groups[6].ToString()) / 60d +
						Convert.ToDouble(match.Groups[7].ToString()) / 3600d;

			if (match.Groups[8].ToString().ToLower() == "w")
			{
				Longitude *= -1;
			}
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
		}

	}
}