﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;


using Gemipedia.Converter.Models;
namespace Gemipedia.Converter.Parser
{
    /// <summary>
    /// Takes specific HtmlElements in WikiHtml and translates them to GemText
    /// </summary>
    public class HtmlTranslater : IArticleLinks
    {
        int ListDepth = 0;

        public ArticleLinkCollection ArticleLinks { get; set; } = new ArticleLinkCollection();

        public string RenderHtml(Element element)
            => RenderContentNode(element, false);

        //most of the time ,we want to ignore all whitepsace text nodes.
        //However, if we are inside of a <PRE>
        //we need to preserve it
        private string RenderChildren(INode node, bool preserveWhitespaceText = false)
            => RenderContentNodes(node.ChildNodes, preserveWhitespaceText);

        private string RenderContentNodes(IEnumerable<INode> nodes, bool preserveWhitespaceText)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var node in nodes)
            {
                sb.Append(RenderContentNode(node, preserveWhitespaceText));
            }
            return sb.ToString();
        }

        private string RenderContentNode(INode current, bool preserveWhitespaceText)
        {
            StringWriter sb = new StringWriter();

            switch (current.NodeType)
            {
                case NodeType.Comment:
                    break;

                case NodeType.Text:
                    if (preserveWhitespaceText)
                    {
                        sb.Write(current.TextContent);
                    }
                    else
                    {
                        //if its not only whitespace add it.
                        if (current.TextContent.Trim().Length > 0)
                        {
                            sb.Write(current.TextContent);
                        }
                        //if its whitepsace, but doesn't have a newline
                        else if (!current.TextContent.Contains('\n'))
                        {
                            sb.Write(current.TextContent);
                        }
                    }
                    break;

                case NodeType.Element:
                    {
                        HtmlElement element = current as HtmlElement;
                        var nodeName = element.NodeName.ToLower();
                        switch (nodeName)
                        {

                            case "a":
                                RecordArticleLink(element);
                                sb.Write(RenderChildren(current, preserveWhitespaceText));
                                break;

                            case "blockquote":
                                RenderChildren(current, preserveWhitespaceText).Trim().Split("\n").ToList()
                                    .ForEach(x => sb.WriteLine($">{x}"));
                                sb.WriteLine();
                                break;

                            case "br":
                                sb.WriteLine();
                                break;

                            case "dd":
                                sb.Write("* ");
                                sb.WriteLine(RenderChildren(current, preserveWhitespaceText));
                                break;

                            case "dt":
                                sb.Write(RenderChildren(current, preserveWhitespaceText));
                                sb.WriteLine(":");
                                break;

                            //we still have to handle headers here, since headers inside
                            //of other complex markup, (like tables) will end up being
                            //rendered here

                            case "h1":
                                sb.WriteLine($"# {CommonUtils.GetHeaderText(element)}");
                                break;

                            case "h2":
                                sb.WriteLine($"## {CommonUtils.GetHeaderText(element)}");
                                break;

                            case "h3":
                            case "h4":
                            case "h5":
                            case "h6":
                                sb.WriteLine($"### {CommonUtils.GetHeaderText(element)}");
                                break;

                            case "i":
                                sb.Write('"');
                                sb.Write(RenderChildren(current, preserveWhitespaceText));
                                sb.Write('"');
                                break;

                            case "li":
                                ProcessLi(element, sb);
                                break;

                            case "ol":
                            case "ul":
                                ListDepth++;
                                sb.Write(RenderChildren(current, preserveWhitespaceText));
                                ListDepth--;
                                break;

                            case "p":
                                {
                                    var innerContent = RenderChildren(current, preserveWhitespaceText).Trim();
                                    if (innerContent.Length > 0)
                                    {
                                        sb.WriteLine(innerContent);
                                        sb.WriteLine();
                                    }
                                }
                                break;

                            case "pre":
                                sb.WriteLine("```");
                                sb.WriteLine(RenderChildren(current, true));
                                sb.WriteLine("```");
                                break;

                            case "span":
                                {
                                    var special = ConvertSpan(element, sb);
                                    if(special.Length >0)
                                    {
                                        sb.Write(special);
                                    } else
                                    {
                                        sb.Write(RenderChildren(current, preserveWhitespaceText));
                                    }
                                }
                                break;

                            case "u":
                                sb.Write('_');
                                sb.Write(RenderChildren(current, preserveWhitespaceText));
                                sb.Write('_');
                                break;

                            //tags to ignore
                            case "link":
                            case "style":
                                break;

                            default:
                                sb.Write(RenderChildren(current, preserveWhitespaceText));
                                break;
                        }
                    }
                    break;
                default:
                    throw new ApplicationException("Unhandled NODE TYPE!");
            }

            return sb.ToString();
        }

        private string ConvertSpan(HtmlElement element, TextWriter sb)
        {
            if (element.ClassList.Contains("mwe-math-element"))
            {
                return SpecialBlockConverter.ConvertMath(element);
            }
            return "";
        }

        private void ProcessLi(HtmlElement element, TextWriter sb)
        { 
            if (ListDepth == 1)
            {
                //if the entire item in a line, make it a link line,
                //otherwise its a bulleted list
                var links = element.QuerySelectorAll("a").ToList();
                if (links.Count > 0 && CommonUtils.ShouldUseLink(links[0]) && element.TextContent.StartsWith(links[0].TextContent))
                {
                    sb.Write($"=> {CommonUtils.ArticleUrl(links[0].GetAttribute("title"))} ");
                    //process inside the A tag so we don't add this link to our list of references
                    sb.WriteLine(RenderChildren(element).Trim());
                }
                else
                {
                    sb.Write("* ");
                    sb.WriteLine(RenderChildren(element).Trim());
                }
            }
            else
            {
                sb.WriteLine();
                sb.Write("* * ");
                sb.WriteLine(RenderChildren(element).Trim());
            }
        }

        private void RecordArticleLink(HtmlElement element)
        {
            if (CommonUtils.ShouldUseLink(element))
            {
                ArticleLinks.AddArticle(element.GetAttribute("title"));
            }
        }

    }
}
