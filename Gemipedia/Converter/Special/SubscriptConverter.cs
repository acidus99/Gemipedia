using System;
using System.Text;
namespace Gemipedia.Converter.Special
{
	public class SubscriptConverter
	{

		public string Original { get; private set; }
		public string Converted { get; private set; }

		StringBuilder buffer = new StringBuilder();

		public bool IsFullyConverted { get; private set; } = true;

		public bool Convert(string s)
        {
			Original = s;
			Converted = "";

			buffer.Clear();
			IsFullyConverted = true;
			foreach(char c in s)
            {
				buffer.Append(ConvertChar(c));
            }
			Converted = buffer.ToString();
			return IsFullyConverted;
        }

		public char ConvertChar(char c)
        {
			switch(c)
            {
				case '0':
					return '\u2080';
				case '1':
					return '\u2081';
				case '2':
					return '\u2082';
				case '3':
					return '\u2083';
				case '4':
					return '\u2084';
				case '5':
					return '\u2085';
				case '6':
					return '\u2086';
				case '7':
					return '\u2087';
				case '8':
					return '\u2088';
				case '9':
					return '\u2089';

				//ASCII plus
				case '+':
				//small plus sign
				case '\uFE62':
				//full width plus sign
				case '\uFF0B':
					return '\u208A';

				//ASCII minus
				case '-':
				//small hyphen-minus
				case '\uFE63':
				//full width plus sign
				case '\uFF0D':
				//minus sign
				case '\u2212':
					return '\u208B';

				//ASCII equals
				case '=':
				//small equals sign
				case '\uFE66':
				//full width equals sign
				case '\uFF1D':
					return '\u208C';

				case '(':
					return '\u208D';
				case ')':
					return '\u208E';
			}
			IsFullyConverted = false;
			return c;
		}
	}
}

