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
                    table.ArticleLinks.MergeCollection(textExtractor.ArticleLinks);
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
                    ColSpan = Convert.ToInt32(cell.GetAttribute("colspan") ?? "1")
                }); ;
            }
        }
    }
}
