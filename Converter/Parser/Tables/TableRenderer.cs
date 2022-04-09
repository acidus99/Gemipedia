using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;
using System.Text;
using Gemipedia.Converter.Parser;

namespace Gemipedia.Converter.Parser.Tables
{
    public class TableRenderer
    {

        int ColumnWidth = 0;

        Table Table;
        StringBuilder buffer;

        private TableRenderer(Table table)
        {
            Table = table;
            buffer = new StringBuilder();
        }

        public string Render()
        {

            if (Table.HasCaption)
            {
                buffer.AppendLine($"### Table: {Table.Caption}");
            }
            buffer.AppendLine("```Table");
            buffer.AppendLine(GenerateDividerLine(Table.Rows[0]));

            foreach (var row in Table.Rows)
            {
                RenderRow(row);
                buffer.AppendLine(GenerateDividerLine(row));
            }
            buffer.AppendLine("```");
            return buffer.ToString();
        }

        private void RenderRow(Row row)
        {
            for (int lineNum = 0, max = row.LineHeight; lineNum < max; lineNum++)
            {
                StringBuilder lineBuffer = new StringBuilder();
                for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                {
                    //leading edge
                    if (cellIndex == 0)
                    {
                        lineBuffer.Append("|");
                    }
                    lineBuffer.Append(row.Cells[cellIndex].FormattedLines[lineNum]);
                    lineBuffer.Append("|");
                }
                buffer.AppendLine(lineBuffer.ToString());
            }
        }

        private string GenerateDividerLine(Row row)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('+');
            for(int i=0; i < row.Cells.Count; i++)
            {
                var cell = row.Cells[i];

                //how wide is the 
                sb.Append(new string('-', cell.FormattedWidth));
                //do we need to add some extra for the cells we skipped?
                sb.Append('+');
            }
            return sb.ToString();
        }

        private void FormatContents()
        {

            ColumnWidth = Math.Max((60 / Table.MaxColumns), 15);

            foreach (var row in Table.Rows)
            {

                foreach (var cell in row.Cells)
                {
                    cell.FormattedLines = FormatCell(cell, ColumnWidth);
                }

                int maxHeight = row.LineHeight;

                foreach (var cell in row.Cells)
                {
                    VerticalPad(cell, maxHeight, ColumnWidth);
                }
            }
        }


        private List<string> FormatCell(Cell cell, int columnWidth)
        {

            var input = cell.IsHeader ? cell.Contents.ToUpper() : cell.Contents;
            int maxWidth = (columnWidth * cell.ColSpan) + (cell.ColSpan - 1);

            List<string> lines = new List<string>();

            if (!input.Contains(" "))
            {
                int start = 0;
                while (start < input.Length)
                {
                    lines.Add(PadCell(input.Substring(start, Math.Min(maxWidth, input.Length - start)), maxWidth, cell.IsHeader));
                    start += maxWidth;
                }
            }
            else
            {
                string[] words = input.Split(' ');

                string line = "";
                foreach (string word in words)
                {
                    if ((line + word).Length > maxWidth)
                    {
                        lines.Add(PadCell(line.Trim(), maxWidth, cell.IsHeader));
                        line = "";
                    }

                    line += string.Format("{0} ", word);
                }

                if (line.Length > 0)
                {
                    lines.Add(PadCell(line.Trim(), maxWidth, cell.IsHeader));
                }
            }
            return lines;
        }

        private string PadCell(string s, int length, bool center)
        {
            int counter = 0;
            for (; s.Length < length;)
            {
                counter++;
                if (center && counter % 2 == 1)
                {
                    s = " " + s;
                }
                else
                {
                    s += " ";
                }
            }
            return s;
        }

        private void VerticalPad(Cell cell, int lines, int width)
        {
            int maxWidth = (width * cell.ColSpan) + (cell.ColSpan - 1);
            for (; cell.FormattedLines.Count < lines;)
            {
                cell.FormattedLines.Add(new string(' ', maxWidth));
            }
        }

        public static string RenderTable(Table Table)
        {
            var renderer = new TableRenderer(Table);
            renderer.FormatContents();
            return renderer.Render();
        }


    }
}
