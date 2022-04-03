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

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser.Tables
{
    public class TableParser
    {

        Table table;

        Row currRow;

        StringBuilder innerText;

        public TableParser()
        {
            table = new Table();
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
                    table.Caption = PrepareText(current.TextContent);
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
                innerText = new StringBuilder();
                ExtractInnerText(cell);


                int colSpan = Convert.ToInt32(cell.GetAttribute("colspan") ?? "1");


                currRow.Cells.Add(new Cell
                {
                    IsHeader = (cell.NodeName == "TH"),
                    Contents = PrepareText(innerText.ToString()),
                    ColSpan = colSpan
                });
            }
        }

        private void ExtractInnerText(INode current)
        {
            switch (current.NodeType)
            {
                case NodeType.Comment:
                    break;

                case NodeType.Text:
                    //if its not only whitespace add it.
                    if (current.TextContent.Trim().Length > 0)
                    {
                        innerText.Append(current.TextContent);
                    }
                    //if its whitepsace, but doesn't have a newline
                    else if (!current.TextContent.Contains('\n'))
                    {
                        innerText.Append(current.TextContent);
                    }
                    break;

                case NodeType.Element:
                    {
                        HtmlElement element = current as HtmlElement;
                        var nodeName = element.NodeName.ToLower();
                        switch (nodeName)
                        {

                            case "br":
                                innerText.AppendLine();
                                break;

                            default:
                                ExtractChildrenText(current);
                                break;
                        }
                    }
                    break;
                default:
                    throw new ApplicationException("Unhandled NODE TYPE!");
            }
        }

        private void ExtractChildrenText(INode element)
            => element.ChildNodes.ToList().ForEach(x => ExtractInnerText(x));


        private string PrepareText(string s)
            => s.Replace("\n", " ").Trim();
    }
}
