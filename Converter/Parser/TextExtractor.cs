using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Gemipedia.Converter.Models;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace Gemipedia.Converter.Parser
{
    /// <summary>
    /// Extracts text
    /// </summary>
    public class TextExtractor
    {

        public ArticleLinkCollection ArticleLinks;
        public StringBuilder buffer = new StringBuilder();
        bool CollapseNewlines;

        public TextExtractor(bool collapseNewlines = false)
        {
            ArticleLinks = new ArticleLinkCollection();
            CollapseNewlines = collapseNewlines;
        }

        public string ExtractInnerText(INode current)
        {
            buffer.Clear();
            ArticleLinks.Clear();
            ExtractInnerTextHelper(current);

            return CollapseNewlines ?
                ConvertNewlines(buffer.ToString()) :
                buffer.ToString();
        }

        private void ExtractInnerTextHelper(INode current)
        {
            switch (current.NodeType)
            {
                case NodeType.Text:
                    //if its not only whitespace add it.
                    if (current.TextContent.Trim().Length > 0)
                    {
                        buffer.Append(current.TextContent);
                    }
                    //if its whitepsace, but doesn't have a newline
                    else if (!current.TextContent.Contains('\n'))
                    {
                        buffer.Append(current.TextContent);
                    }
                    break;

                case NodeType.Element:
                    {
                        HtmlElement element = current as HtmlElement;
                        var nodeName = element.NodeName.ToLower();
                        switch (nodeName)
                        {

                            case "a":
                                ArticleLinks.Add(element);
                                ExtractChildrenText(current);
                                break;

                            case "br":
                                buffer.AppendLine();
                                break;

                            default:
                                ExtractChildrenText(current);
                                break;
                        }
                    }
                    break;
            }
        }

        private void ExtractChildrenText(INode element)
            => element.ChildNodes.ToList().ForEach(x => ExtractInnerTextHelper(x));

        private string ConvertNewlines(string s)
            => s.Replace("\n", " ").Trim();

    }
}
