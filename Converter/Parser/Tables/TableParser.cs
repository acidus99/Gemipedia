using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser.Tables
{
    public class TableParser : IArticleLinks
    {

        Row currRow;
        Table table;
        TextExtractor textExtractor;

        public ArticleLinkCollection ArticleLinks { get; private set; }

        public TableParser()
        {
            table = new Table();
            textExtractor = new TextExtractor(collapseNewlines: true);
            ArticleLinks = new ArticleLinkCollection();
        }

        public Table ParseTable(HtmlElement element)
        {
            ParseChildren(element);
            AppendRow();
            //go back and place any rowspan placeholder cells
            UpdateForRowSpans();
            return table;
        }

        private void ParseChildren(HtmlElement element)
            => element.Children.ToList().ForEach(x => ParseTag((HtmlElement) x));

        private void ParseTag(HtmlElement current)
        {

            switch(current.NodeName.ToLower())
            {
                case "caption":
                    table.Caption = textExtractor.GetText(current);
                    ArticleLinks.MergeCollection(textExtractor.ArticleLinks);
                    break;

                case "tr":
                    {
                        AppendRow();
                        currRow = new Row();
                        ParseChildren(current);
                        break;
                    }

                case "td":
                case "th":
                    AddCell(current);
                    break;

                //pass through
                case "tbody":
                case "tfoot":
                case "thead":
                    ParseChildren(current);
                    break;
            }
        }

        private void AppendRow()
        {
            if (currRow != null)
            {
                table.Rows.Add(currRow);
            }
        }

        private void AddCell(HtmlElement cell)
        {
            if (currRow != null)
            {
                string contents = textExtractor.GetText(cell);
                ArticleLinks.MergeCollection(textExtractor.ArticleLinks);

                currRow.Cells.Add(new Cell
                {
                    IsHeader = (cell.NodeName == "TH"),
                    Contents = contents,
                    ColSpan = Convert.ToInt32(cell.GetAttribute("colspan") ?? "1"),
                    RowSpan = Convert.ToInt32(cell.GetAttribute("rowspan") ?? "1"),
                    IsRowSpanHolder = false
                });
            }
        }

        private void UpdateForRowSpans()
        {
            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
            {
                List<Cell> newRow = new List<Cell>();
                Queue<Cell> oldRow = new Queue<Cell>(table.Rows[rowIndex].Cells);
                Queue<Cell> prevRow = new Queue<Cell>(table.Rows[rowIndex - 1].Cells);
                while (prevRow.Count > 0)
                {
                    var prevRowCell = prevRow.Dequeue();

                    if (prevRowCell.RowSpan > 1)
                    {
                        //push on a placeholder
                        newRow.Add(new Cell
                        {
                            IsRowSpanHolder = true,
                            RowSpan = prevRowCell.RowSpan - 1,
                            ColSpan = prevRowCell.ColSpan,
                            IsHeader = prevRowCell.IsHeader,
                        });
                    }
                    else
                    {
                        for (int i = 0; i < prevRowCell.ColSpan; i++)
                        {
                            //pull cell from current row == the colspan of 
                            if (oldRow.Count > 0)
                            {
                                var cell = oldRow.Dequeue();
                                newRow.Add(cell);
                                i += cell.ColSpan - 1;
                            }
                        }
                    }
                }
                //There should not be anything left in oldRow. If so, the
                //number of cells in the source table were mismatched, so try
                //and handle that
                while (oldRow.Count > 0)
                {
                    newRow.Add(oldRow.Dequeue());
                }
                table.Rows[rowIndex].Cells = newRow;
            }
        }

    }
}
