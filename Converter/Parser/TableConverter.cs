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
using Gemipedia.Converter.Parser.Tables;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser
{
    public static class TableConverter
    {

        public static ContentItem Convert(HtmlElement element)
        {
            TableParser tableParser = new TableParser();
            var table = tableParser.ParseTable(element);

            //render it to string
            FormatContents(table);

            var ci = new ContentItem
            {
                Content = RenderContents(table)
            };

            return ci;
        }


        private static string RenderContents(Table table)
        {
            StringBuilder sb = new StringBuilder();
            int cellSize = table.Rows[0].Cells[0].FormattedLines[0].Length;

            int renderWidth = ((cellSize + 1) * table.NumColumns) + 1;
            

            if(table.HasCaption)
            {
                sb.AppendLine($"### Table: {table.Caption}");
            }
            sb.AppendLine("```table");
            sb.AppendLine(GenerateDividerLine(table.Rows[0], cellSize));
            foreach(var row in table.Rows)
            {
                for(int lineNum = 0, max = row.LineHeight; lineNum < max; lineNum++)
                {
                    for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                    {
                        //leading edge
                        if(cellIndex == 0)
                        {
                            sb.Append("|");
                        }
                        sb.Append(row.Cells[cellIndex].FormattedLines[lineNum]);
                        sb.Append("|");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine(GenerateDividerLine(row, cellSize));
            }
            sb.AppendLine("```");
            return sb.ToString();
        }

        private static string GenerateDividerLine(Row row, int colWidth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("+");
            foreach(var cell in row.Cells)
            {
                sb.Append(new string('-', colWidth * cell.ColSpan));
                sb.Append("+");
            }
            return sb.ToString();
        }

        private static void FormatContents(Table table)
        {
            var columnWidth = Math.Max((60 / table.NumColumns), 15);

            foreach(var row in table.Rows)
            {
                foreach(var cell in row.Cells)
                {
                    cell.FormattedLines = FormatCell(cell, columnWidth);
                }

                int maxHeight = row.LineHeight;
                
                foreach (var cell in row.Cells)
                {
                    VerticalPad(cell, maxHeight, columnWidth);
                }
            }
        }


        private static List<string> FormatCell(Cell cell, int maxCharacters)
        {

            var input = cell.IsHeader ? cell.Contents.ToUpper() : cell.Contents;

            List<string> lines = new List<string>();

            if (!input.Contains(" "))
            {
                int start = 0;
                while (start < input.Length)
                {
                    lines.Add(PadCell(input.Substring(start, Math.Min(maxCharacters, input.Length - start)), maxCharacters, cell.IsHeader));
                    start += maxCharacters;
                }
            }
            else
            {
                string[] words = input.Split(' ');

                string line = "";
                foreach (string word in words)
                {
                    if ((line + word).Length > maxCharacters)
                    {
                        lines.Add(PadCell(line.Trim(), maxCharacters, cell.IsHeader));
                        line = "";
                    }

                    line += string.Format("{0} ", word);
                }

                if (line.Length > 0)
                {
                    lines.Add(PadCell(line.Trim(), maxCharacters, cell.IsHeader));
                }
            }
            return lines;
        }


        private static string PadCell(string s, int length, bool center)
        {
            int counter = 0;
            for(;s.Length < length;)
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

        private static void VerticalPad(Cell cell, int lines, int width)
        {
            for (; cell.FormattedLines.Count < lines;)
            {
                cell.FormattedLines.Add(new string(' ', width));
            }
        }


    }

}
