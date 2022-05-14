using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Gemipedia.Converter.Models;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace Gemipedia.Converter.Parser
{
    /// <summary>
    /// Extracts text
    /// </summary>
    public class TextExtractor : ITextContent
    {
        public string Content
            => ShouldCollapseNewlines ?
                        CollapseNewlines(buffer.ToString()) :
                        buffer.ToString();

        public ArticleLinkCollection Links { get; private set; } = new ArticleLinkCollection();

        public bool ShouldCollapseNewlines { get; set; } = false;
        public bool ShouldConvertImages { get; set; } = false;

        private static readonly Regex whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        private StringBuilder buffer = new StringBuilder();


        public void Extract(params INode[] nodes)
            => Extract(nodes.Where(x => x != null).FirstOrDefault());

        public void Extract(INode current)
        {
            Clear();
            if (current == null)
            {
                //nothing to do
                return;
            }
            ExtractInnerTextHelper(current);
        }

        private void Clear()
        {
            Links.Clear();
            buffer.Clear();
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
                                Links.Add(element);
                                ExtractChildrenText(current);
                                break;

                            case "br":
                                buffer.AppendLine();
                                break;

                            case "img":
                                if(ShouldConvertImages)
                                {
                                    buffer.Append(ConvertImage(element));
                                }
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

        //converts newlines to spaces. since that can create runs of whitespace,
        //remove those is they exist
        private string CollapseNewlines(string s)
            => CollapseSpaces(ConvertNewlines(s));

        private string ConvertNewlines(string s)
            => s.Replace("\n", " ").Trim();

        private string CollapseSpaces(string s)
            => whitespace.Replace(s, " ");

        private string ConvertImage(HtmlElement element)
        {
            var alt = element.GetAttribute("alt");
            return !string.IsNullOrEmpty(alt) ?
                $"[Image: {alt}] " :
                "[Image] ";
        }

    }
}
