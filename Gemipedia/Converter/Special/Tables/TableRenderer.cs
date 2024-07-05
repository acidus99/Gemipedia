using System;
using System.Collections.Generic;
using System.Text;

namespace Gemipedia.Converter.Special.Tables;

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

    private string Render()
    {
        if (Table.HasCaption)
        {
            buffer.AppendLine($"### Table: {Table.Caption}");
        }
        buffer.AppendLine("```Table");
        buffer.AppendLine(GenerateDividerLine(Table.Rows[0], true));

        for (int i = 0; i < Table.Rows.Count; i++)
        {
            var row = Table.Rows[i];
            RenderRow(row);
            //are we on the last row?
            buffer.AppendLine(GenerateDividerLine(row, (i + 1) == Table.Rows.Count));
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

    private string GenerateDividerLine(Row row, bool IsEdge = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('+');
        for (int i = 0; i < row.Cells.Count; i++)
        {
            //do we need to leave it open or draw a horizontal line?
            //for the top/bottom edges, we always draw the line
            var cell = row.Cells[i];
            if (!IsEdge && cell.RowSpan > 1)
            {
                sb.Append(new string(' ', cell.FormattedWidth));
            }
            else
            {
                sb.Append(new string('-', cell.FormattedWidth));
            }
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
        //is this a rowspan placeholder?
        if (cell.IsRowSpanHolder)
        {
            return FormatPlaceholder(cell, columnWidth);
        }

        var input = cell.IsHeader ? cell.Contents.ToUpper() : cell.Contents;
        int maxWidth = (columnWidth * cell.ColSpan) + (cell.ColSpan - 1);

        List<string> lines = new List<string>();

        string[] words = input.Split(' ');

        string line = "";
        int lineLength = 0;
        foreach (string word in words)
        {

            int wordLength = UnicodeString.GetWidth(word);
            //do we have extra-wide characters?
            bool hasWideCharacters = (wordLength != word.Length);
            //can the word fit?
            if (wordLength > maxWidth)
            {
                //nope, we are going to need to hard slice this word to fit to the width
                //this is complex if we have wide characters

                //Step 1: flush anything still in the buffer
                if (lineLength > 0)
                {
                    lines.Add(PadCell(line.Trim(), maxWidth, cell.IsHeader));
                    line = "";
                    lineLength = 0;
                }

                //step 2: determine the amount of characters to use in each hard slice
                int substringLength = maxWidth;
                if (hasWideCharacters && word.Length < maxWidth)
                {
                    //if we have wide characters, we need to do a smaller
                    substringLength = word.Length / 2;
                }

                int start = 0;
                while (start < word.Length)
                {
                    lines.Add(PadCell(word.Substring(start, Math.Min(substringLength, word.Length - start)), maxWidth, cell.IsHeader));
                    start += substringLength;
                }
                continue;
            }
            //will the buffer be too big? if so, flush it
            if ((lineLength + wordLength) > maxWidth)
            {
                lines.Add(PadCell(line.Trim(), maxWidth, cell.IsHeader));
                line = "";
                lineLength = 0;
            }
            line += word;
            lineLength += wordLength;
            if (wordLength + 1 <= maxWidth)
            {
                line += " ";
                lineLength += 1;
            }
        }
        //flush any remaining in buffer
        if (lineLength > 0)
        {
            lines.Add(PadCell(line.Trim(), maxWidth, cell.IsHeader));
        }
        return lines;
    }

    private List<string> FormatPlaceholder(Cell cell, int columWidth)
    {
        int maxWidth = (columWidth * cell.ColSpan) + (cell.ColSpan - 1);
        var ret = new List<string>();
        ret.Add(new string(' ', maxWidth));
        return ret;
    }

    private string PadCell(string s, int length, bool center)
    {
        int counter = 0;
        int initialLength = UnicodeString.GetWidth(s);
        int addedLength = 0;
        for (; initialLength + addedLength < length;)
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
            addedLength++;
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
        if (Table.IsEmpty)
        {
            return "";
        }
        var renderer = new TableRenderer(Table);
        renderer.FormatContents();
        return renderer.Render();
    }
}