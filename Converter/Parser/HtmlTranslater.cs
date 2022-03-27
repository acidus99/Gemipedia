using System;
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
    public class HtmlTranslater : ILinkedArticles
    {
        int ListDepth = 0;
        LinkedArticles links = new LinkedArticles();

        public List<string> LinkedArticles
            => links.GetLinks();

        public string RenderHtml(Element element)
            => RenderContentNode(element);

        private string RenderChildren(INode node)
            => RenderContentNodes(node.ChildNodes);


        private string RenderContentNodes(IEnumerable<INode> nodes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var node in nodes)
            {
                sb.Append(RenderContentNode(node));
            }
            return sb.ToString();
        }

        private string RenderContentNode(INode current)
        {
            StringWriter sb = new StringWriter();

            switch (current.NodeType)
            {
                case NodeType.Comment:
                    break;

                case NodeType.Text:
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
                    break;

                case NodeType.Element:
                    {
                        HtmlElement element = current as HtmlElement;
                        var nodeName = element.NodeName.ToLower();
                        switch (nodeName)
                        {

                            case "a":
                                RecordArticleLink(element);
                                sb.Write(RenderChildren(current));
                                break;

                            case "blockquote":
                                RenderChildren(current).Trim().Split("\n").ToList()
                                    .ForEach(x => sb.WriteLine($">{x}"));
                                sb.WriteLine();
                                break;

                            case "br":
                                sb.WriteLine();
                                break;

                            case "dd":
                                sb.Write("* ");
                                sb.WriteLine(RenderChildren(current));
                                break;

                            case "dt":
                                sb.Write(RenderChildren(current));
                                sb.WriteLine(":");
                                break;

                            case "i":
                                sb.Write('"');
                                sb.Write(RenderChildren(current));
                                sb.Write('"');
                                break;

                            case "u":
                                sb.Write('_');
                                sb.Write(RenderChildren(current));
                                sb.Write('_');
                                break;

                            case "ol":
                            case "ul":
                                ListDepth++;
                                sb.Write(RenderChildren(current));
                                ListDepth--;
                                break;

                            case "li":
                                ProcessLi(element, sb);
                                break;

                            case "p":
                                {
                                    var innerContent = RenderChildren(current).Trim();
                                    if (innerContent.Length > 0)
                                    {
                                        sb.WriteLine(innerContent);
                                        sb.WriteLine();
                                    }
                                }
                                break;

                            //tags to ignore
                            case "link":
                            case "style":
                                break;

                            default:
                                sb.Write(RenderChildren(current));
                                break;
                        }
                    }
                    break;
                default:
                    throw new ApplicationException("Unhandled NODE TYPE!");
            }

            return sb.ToString();
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
                links.AddLink(element.GetAttribute("title"));
            }
        }

    }
}
