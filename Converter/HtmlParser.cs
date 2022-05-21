using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Filter;
using Gemipedia.Converter.Special;
using Gemipedia.Models;

namespace Gemipedia.Converter
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

        private bool inPreformatted = false;

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
            FlushBuffer();           
            return items;
        }

        private void AddItem(SectionItem item)
        {
            if(item != null)
            {
                FlushBuffer();
                items.Add(item);
            }
        }

        private void FlushBuffer()
        {
            if (buffer.HasContent)
            {
                items.Add(new ContentItem(buffer));
                buffer.Reset();
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
            if (inPreformatted)
            {
                buffer.Append(textNode.TextContent);
            }
            else
            {

                //if its not only whitespace add it.
                if (textNode.TextContent.Trim().Length > 0)
                {
                    if (buffer.AtLineStart)
                    {
                        buffer.Append(textNode.TextContent.TrimStart());
                    }
                    else
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
        }

        private void ProcessHtmlElement(HtmlElement element)
        {
            var nodeName = element?.NodeName.ToLower();

            if (!ShouldProcessElement(element, nodeName))
            {
                return;
            }

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
                    int size = buffer.Content.Length;
                    ParseChildern(element);
                    //make sure the paragraph ends with a new line
                    buffer.EnsureAtLineStart();
                    if (buffer.Content.Length > size)
                    {
                        //add another blank line if this paragraph had content
                        buffer.AppendLine();
                    }
                    break;

                case "pre":
                    buffer.EnsureAtLineStart();
                    buffer.AppendLine("```");
                    inPreformatted = true;
                    ParseChildern(element);
                    inPreformatted = false;
                    buffer.AppendLine("```");
                    break;

                case "table":
                    ProcessTable(element);
                    break;

                case "u":
                    buffer.Append("_");
                    ParseChildern(element);
                    buffer.Append("_");
                    break;

                default:
                    ProcessGenericTag(element);
                    break;
            }
        }

        private bool ShouldProcessElement(HtmlElement element,string normalizedTagName)
        {
            //A MathElement is of type element, but it not an HtmlElement
            //so it will be null
            if (element == null)
            {
                return false;
            }

            //see if we are explicitly filtering
            if (!DomFilter.Global.IsElementAllowed(element, normalizedTagName))
            {
                return false;
            }

            //is it visible?
            if (IsInvisible(element))
            {
                return false;
            }

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

            //a navigation note?
            if (div.GetAttribute("role") == "note" && div.ClassList.Contains("navigation-not-searchable"))
            {
                AddItem(NavigationParser.ConvertNavigationNote(div));
                return;
            }

            if (div.ClassList.Contains("timeline-wrapper"))
            {
                AddItem(MediaParser.ConvertTimeline(div));
                return;
            }

            //fall through to generic handling
            ProcessGenericTag(div);
        }

        private void ProcessGenericTag(HtmlElement element)
        {
            //is this a math element?
            if(element.ClassList.Contains("mwe-math-element"))
            {
                //math elements have to be displayed at the start of the like
                buffer.EnsureAtLineStart();
                buffer.AppendLine(MathConverter.ConvertMath(element));
            }

            if (ShouldDisplayAsBlock(element))
            {
                buffer.EnsureAtLineStart();
                ParseChildern(element);
                buffer.EnsureAtLineStart();
            }
            else
            {
                ParseChildern(element);
            }
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

        private void ProcessTable(HtmlElement table)
        {
            //is it a data table?
            if (table.ClassList.Contains("wikitable"))
            {
                AddItem(WikiTableConverter.ConvertWikiTable(table));
                return;
            }

            if (table.ClassList.Contains("infobox"))
            {
                InfoboxParser parser = new InfoboxParser();
                AddItem(parser.Parse(table));
                return;
            }

            //is it a table just used to create a multicolumn view?
            if (IsMulticolumnLayoutTable(table))
            {
                ParseMulticolmnTable(table);
                return;
            }
        }

        private bool ShouldDisplayAsBlock(HtmlElement element)
        {
            var nodeName = element.NodeName.ToLower();
            if (!blockElements.Contains(nodeName))
            {
                return false;
            }
            //its a block, display it as inline?
            return !IsInline(element);
        }

        private bool IsInline(HtmlElement element)
            => element.GetAttribute("style")?.Contains("display:inline") ?? false;

        private bool IsMulticolumnLayoutTable(HtmlElement element)
            => element.GetAttribute("role") == "presentation" &&
                element.ClassList.Contains("multicol") &&
                element.HasChildNodes &&
                element.Children[0].NodeName == "TBODY" &&
                element.Children[0].HasChildNodes &&
                element.Children[0].Children[0].NodeName == "TR";

        private void ParseMulticolmnTable(HtmlElement table)
        {
            table.Children[0].Children[0].Children
                .Where(x => x.NodeName == "TD").ToList()
                .ForEach(x => ParseChildern(x));
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
