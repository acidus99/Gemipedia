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

using Gemipedia.NGConverter.Models;
using Gemipedia.NGConverter.Special;

namespace Gemipedia.NGConverter
{
    /// <summary>
    /// parses HTML nodes into Section Items
    /// </summary>
    public class HtmlParser
    {

        string[] blockElements = new string[] { "address", "article", "aside", "blockquote", "canvas", "dd", "div", "dl", "dt", "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "li", "main", "nav", "noscript", "ol", "p", "pre", "section", "table", "tfoot", "ul", "video" };

        public List<SectionItem> ConvertedItems = new List<SectionItem>();

        Buffer buffer = new Buffer();

        bool InBlockquote = false;

        public HtmlParser()
        {
        }

        public void Parse(INode current)
        {
            ParseHelper(current);
            //flush any remaining content
            FlushContentBlock();
        }

        private void AddItem(SectionItem item)
        {
            if(item != null)
            {
                ConvertedItems.Add(item);
            }
        }

        private void ParseHelper(INode current)
        {
            switch (current.NodeType)
            {
                case NodeType.Text:
                    ProcessTextNode(current);
                    break;

                case NodeType.Element:
                    ProcessHtmlElement(current as HtmlElement);
                    break;
            }
        }


        private void ParseChildern(INode node)
        {
            foreach (var child in node.ChildNodes)
            {
                ParseHelper(child);
            }
        }

        private void ProcessTextNode(INode textNode)
        {
            //if its not only whitespace add it.
            if (textNode.TextContent.Trim().Length > 0)
            {
                buffer.Append(textNode.TextContent);
            }
            //if its whitepsace, but doesn't have a newline
            else if (!textNode.TextContent.Contains('\n'))
            {
                buffer.Append(textNode.TextContent);
            }
        }

        private void ProcessHtmlElement(HtmlElement element)
        {
            if (!ShouldProcessElement(element))
            {
                return;
            }

            var nodeName = element.NodeName.ToLower();

            switch (nodeName)
            {
                case "a":
                    buffer.Links.Add(element);
                    ParseChildern(element);
                    break;

                case "blockquote":
                    ProcessBlockquote(element);
                    break;

                case "div":
                    ProcessDiv(element);
                    break;

                case "li":
                    ProcessLi(element);
                    break;


                case "p":
                    FlushContentBlock(false);
                    ParseChildern(element);
                    FlushContentBlock();
                    break;

                case "table":
                    break;

                //will do things here
                default:
                    if (IsBlockElement(nodeName))
                    {
                        FlushContentBlock(false);
                        ParseChildern(element);
                        FlushContentBlock(false);
                    }
                    else
                    {
                        ParseChildern(element);
                    }
                    break;
            }
        }

        private bool ShouldProcessElement(HtmlElement element)
        {
            //A MathElement is of type element, but it not an HtmlElement
            //so it will be null
            if (element == null)
            {
                return false;
            }

            if (IsInvisible(element))
            {
                return false;
            }

            //could apply other skip rules here

            return true;
        }

        private bool IsInvisible(HtmlElement element)
           => element.GetAttribute("style")?.Contains("display:none") ?? false;

        private bool IsBlockElement(string tagName)
            => blockElements.Contains(tagName);

        private void ProcessBlockquote(HtmlElement blockquote)
        {
            InBlockquote = true;
            ParseChildern(blockquote);
            FlushContentBlock();
            InBlockquote = false;
        }

        private void ProcessDiv(HtmlElement div)
        {
            //is it a media div?
            if (div.ClassList.Contains("thumb") && !div.ClassList.Contains("locmap"))
            {
                AddItem(MediaParser.Convert(div, div.QuerySelector(".thumbcaption")));
                return;
            }

            //for now, don't process DIV children
            
        }

        private void ProcessLi(HtmlElement li)
        {
            buffer.AllowNewlines = false;
            ParseChildern(li);
            if (buffer.HasContent)
            {
                AddItem(new ContentItem
                {
                    ArticleLinks = buffer.Links,
                    Content = $"* {buffer.Content.Trim()}\n"
                });
                buffer.Reset();
            }
            buffer.AllowNewlines = true;
        }

        private void FlushContentBlock(bool addTrailingNewline = true)
        {
            if (buffer.HasContent)
            {
                var content = buffer.Content.Trim();
                content += "\n";
                content = ApplyState(content);
                if (addTrailingNewline)
                {
                    //its a block so add an empty line to the end
                    content += "\n";
                }
                AddItem(new ContentItem
                {
                    ArticleLinks = buffer.Links,
                    Content = content
                });
                buffer.Reset();
            }
        }

        private string ApplyState(string content)
        {
            if(InBlockquote)
            {
                var sb = new StringBuilder(content.Length + 10);
                var lines = content.Trim().Split("\n").ToList();
                lines.ForEach(x => sb.AppendLine($">{x}"));
                return sb.ToString();
            }
            //otherwise return original content
            return content;
        }
    }
}
