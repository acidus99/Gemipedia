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

        private List<SectionItem> items = new List<SectionItem>();

        private int listDepth = 0;

        Buffer buffer = new Buffer();

        /// <summary>
        /// should we try and convert list items to links?
        /// </summary>
        public bool ConvertListItems { get; set; } = true;


        public void Parse(INode current)
        {
            ParseHelper(current);
        }

        public List<SectionItem> GetItems()
        {
            //still have anything in the buffer?
            //if so flush it
            if(buffer.HasContent)
            {
                AddItem(new ContentItem
                {
                    ArticleLinks = buffer.Links,
                    Content = buffer.Content
                });
                buffer.Reset();
            }
            return items;
        }

        private void AddItem(SectionItem item)
        {
            if(item != null)
            {
                items.Add(item);
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
                if(buffer.AtLineStart)
                {
                    buffer.Append(textNode.TextContent.TrimStart());
                } else
                {
                    buffer.Append(textNode.TextContent);
                }
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
                    buffer.EnsureAtLineStart();
                    buffer.InBlockquote = true;
                    ParseChildern(element);
                    buffer.InBlockquote = false;
                    break;

                case "br":
                    buffer.AppendLine();
                    break;

                case "dd":
                    buffer.EnsureAtLineStart();
                    buffer.SetLineStart("* ");
                    ParseChildern(element);
                    buffer.EnsureAtLineStart();
                    break;

                case "div":
                    ProcessDiv(element);
                    break;

                case "dt":
                    buffer.EnsureAtLineStart();
                    ParseChildern(element);
                    if (!buffer.AtLineStart)
                    {
                        buffer.AppendLine(":");
                    }
                    break;

                case "i":
                    buffer.Append("\"");
                    ParseChildern(element);
                    buffer.Append("\"");
                    break;

                case "li":
                    ProcessLi(element);
                    break;

                case "ol":
                case "ul":
                    //block element
                    buffer.EnsureAtLineStart();
                    listDepth++;
                    ParseChildern(element);
                    listDepth--;
                    buffer.EnsureAtLineStart();
                    break;

                case "p":
                    buffer.EnsureAtLineStart();
                    ParseChildern(element);
                    //make sure the paragraph ends with a new line
                    buffer.EnsureAtLineStart();
                    //add another blank line
                    buffer.AppendLine();
                    break;

                case "table":
                    break;

                case "u":
                    buffer.Append("_");
                    ParseChildern(element);
                    buffer.Append("_");
                    break;

                //will do things here
                default:
                    if (IsBlockElement(nodeName))
                    {
                        buffer.EnsureAtLineStart();
                        ParseChildern(element);
                        buffer.EnsureAtLineStart();
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

            if (TryConvertingToLink(li))
            {
                return;
            }

            if (listDepth == 1)
            {
                buffer.EnsureAtLineStart();
                buffer.SetLineStart("* ");
                ParseChildern(li);
                buffer.EnsureAtLineStart();

            }
            else
            {
                buffer.EnsureAtLineStart();
                buffer.SetLineStart("* * ");
                ParseChildern(li);
                buffer.EnsureAtLineStart();
            }
        }

        /// <summary>
        /// See if a list element can be converted to a link and output it to the buffer.
        /// Returns if list items was converted to a link or not
        /// </summary>
        /// <param name="li"></param>
        /// <returns></returns>
        private bool TryConvertingToLink(HtmlElement li)
        {
            if (ConvertListItems)
            {
                //if an list item starts with a link, make it a link
                var links = li.QuerySelectorAll("a").ToList();
                if (links.Count > 0 && ArticleLinkCollection.ShouldUseLink(links[0]) && li.TextContent.StartsWith(links[0].TextContent))
                {
                    buffer.EnsureAtLineStart();
                    buffer.SetLineStart($"=> {CommonUtils.ArticleUrl(links[0].GetAttribute("title"))} ");
                    ParseChildern(li);
                    buffer.EnsureAtLineStart();
                    return true;
                }
            }
            return false;
        }
    }
}
