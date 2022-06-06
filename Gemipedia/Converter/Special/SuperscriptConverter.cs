﻿using System;
using System.Text;
namespace Gemipedia.Converter.Special
{
	public class SuperscriptConverter
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
				if(!IsFullyConverted)
                {
					return false;
                }
            }
			Converted = buffer.ToString();
			return IsFullyConverted;
        }

		public char ConvertChar(char c)
        {
			switch(c)
            {
				case '0':
					return '\u2070';
				case '1':
					return '\u00B9';
				case '2':
					return '\u00B2';
				case '3':
					return '\u00B3';
				case '4':
					return '\u2074';
				case '5':
					return '\u2075';
				case '6':
					return '\u2076';
				case '7':
					return '\u2077';
				case '8':
					return '\u2078';
				case '9':
					return '\u2079';

				//ASCII plus
				case '+':
				//small plus sign
				case '\uFE62':
				//full width plus sign
				case '\uFF0B':
					return '\u207A';

				//ASCII minus
				case '-':
				//small hyphen-minus
				case '\uFE63':
				//full width plus sign
				case '\uFF0D':
				//minus sign
				case '\u2212':
					return '\u207B';

				//ASCII equals
				case '=':
				//small equals sign
				case '\uFE66':
				//full width equals sign
				case '\uFF1D':
					return '\u207C';

				case '(':
					return '\u207D';
				case ')':
					return '\u207E';

				case 'a':
					return '\u1D43';
				case 'b':
					return '\u1D47';
				case 'c':
					return '\u1D9C';
				case 'd':
					return '\u1D48';
				case 'e':
					return '\u1D49';
				case 'f':
					return '\u1DA0';
				case 'g':
					return '\u1D4D';
				case 'h':
					return '\u02B0';
				case 'i':
					return '\u2071';
				case 'j':
					return '\u02B2';
				case 'k':
					return '\u1D4F';
				case 'l':
					return '\u02E1';
				case 'm':
					return '\u1D50';
				case 'n':
					return '\u207F';
				case 'o':
					return '\u1D52';
				case 'p':
					return '\u1D56';
				// there is no widely support Q subscript
				//case 'q':
					
				case 'r':
					return '\u02B3';
				case 's':
					return '\u02E2';
				case 't':
					return '\u1D57';
				case 'u':
					return '\u1D58';
				case 'v':
					return '\u1D5B';
				case 'w':
					return '\u02B7';
				case 'x':
					return '\u02E3';
				case 'y':
					return '\u02B8';
				case 'z':
					return '\u1DBB';
			}
			IsFullyConverted = false;
			return c;
		}
	}
}
