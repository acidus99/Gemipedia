using System;
using System.Linq;
using System.Collections.Generic;
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
        public string Contents = "";
        public int ColSpan = 1;

        public List<string> FormattedLines;

        public int LineHeight
            => FormattedLines?.Count ?? 0;

        public int FormattedWidth
            => (FormattedLines?.Count > 0) ? FormattedLines[0].Length : 0;
    }
}
