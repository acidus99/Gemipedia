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

				//some letters
				case 'a':
					return '\u2090';
				case 'e':
					return '\u2091';
				case 'h':
					return '\u2095';
				case 'i':
					return '\u1D62';
				case 'j':
					return '\u2C7C';
				case 'k':
					return '\u2096';
				case 'l':
					return '\u2097';
				case 'm':
					return '\u2098';
				case 'n':
					return '\u2099';
				case 'o':
					return '\u2092';
				case 'p':
					return '\u209A';
				case 'r':
					return '\u1D63';
				case 's':
					return '\u209B';
				case 't':
					return '\u209C';
				case 'u':
					return '\u1D64';
				case 'v':
					return '\u1D65';
				case 'x':
					return '\u2093';

			}
			IsFullyConverted = false;
			return c;
		}
	}
}

