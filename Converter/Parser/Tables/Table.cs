using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser.Tables
{

    public class Table
    {
        public string Caption = "";
        public List<Row> Rows = new List<Row>();

        public bool HasCaption
            => Caption.Length > 0;

        public int MaxColumns
            => Rows.Max(x => x.Cells.Count);
    }

    public class Row
    {
        public List<Cell> Cells = new List<Cell>();

        public int LineHeight =>
            Cells.Max(x => x.LineHeight);
    }

    public class Cell
    {
        public bool IsHeader = false;

        private string contents = "";
        public string Contents
        {
            get => contents;
            set
            {
                contents = StripZeroWidth(value);
            }
        }
          
        public int ColSpan = 1;

        public int RowSpan = 1;
        //is this a dummy cell, only present to hold open a row spanning cell from a row above?
        public bool IsRowSpanHolder = false;

        public List<string> FormattedLines;

        public int LineHeight
            => FormattedLines?.Count ?? 0;

        public int FormattedWidth
            => (FormattedLines?.Count > 0) ? FormattedLines[0].Length : 0;

        /// <summary>
        /// removes any zero-width unicode characters from the string
        /// these will mess with our column layout since .Lenth with return a number
        /// longer than the number of characters that are rendered
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string StripZeroWidth(string s)
        {
            //Replace("\u200b", "") does not appear to work for these unicode characters
            //do it char by char
            var sb = new StringBuilder(s.Length);
            foreach(char c in s)
            {
                if(c == '\u200b' || c == '\ufeff')
                {
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
