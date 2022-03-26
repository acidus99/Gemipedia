using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using System.Text;

using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace Gemipedia.Converter
{
    public class GemTextRenderer
    {
        ConverterSettings Settings;
        TextWriter Writer;

        int ListDepth = 0;

        public GemTextRenderer(ConverterSettings settings, TextWriter writer)
        {
            Settings = settings;
            Writer = writer;
        }

        public void RenderArticle(string title, List<Section> sections)
        {
            RenderArticleTitle(title);
            foreach(var section in sections)
            {
                Writer.Write(RenderSection(section));
            }
        }

        private void RenderArticleTitle(string title)
        {
            Writer.WriteLine($"# {title}");
            Writer.WriteLine();
        }

        private string RenderSection(Section section)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(RenderContentNodes(section.ContentNodes));
            
            foreach (var subSection in section.SubSections)
            {
                sb.Append(RenderSection(subSection));
            }

            //if a section has no content, don't write anything
            if(sb.Length == 0)
            {
                return "";
            }

            StringBuilder completeSection = new StringBuilder();
            if (!section.IsSpecial)
            {
                if (section.SectionDepth == 2)
                {
                    completeSection.AppendLine($"## {section.Title}");
                }
                else
                {
                    //all other sections are at a level 3
                    completeSection.AppendLine($"### {section.Title}");
                }
            }
            completeSection.Append(sb.ToString());
            return completeSection.ToString();
        }

        private string RenderContentNodes(IEnumerable<INode> nodes)
        {
            StringBuilder sb = new StringBuilder();

            foreach(var node in nodes)
            {
                sb.Append(RenderContentNode(node));
            }
            return sb.ToString();
        }

        private string RenderChildren(INode node)
            => RenderContentNodes(node.ChildNodes);

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
                    else if (!current.TextContent.Contains("\n"))
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
                                //RecordHyperlink(element);
                                sb.Write(RenderChildren(current));
                                break;

                            case "blockquote":
                                RenderContentNodes(current.ChildNodes).Trim().Split("\n").ToList()
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

                            case "div":
                                ProcessDiv(element, sb);
                                break;

                            case "table":
                                ProcessTable(element, sb);
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

        private string RewriteMediaUrl(string url)
            => $"{Settings.MediaProxyUrl}?{WebUtility.UrlEncode(url)}";

        private string ArticleUrl(string title)
            => $"{Settings.ArticleUrl}?{WebUtility.UrlEncode(title)}";

        private void ProcessDiv(HtmlElement element, TextWriter sb)
        {
            //is it a media div?
            if (Settings.ShouldConvertMedia)
            {
                if (element.ClassList.Contains("thumb") && !element.ClassList.Contains("locmap"))
                {
                    var url = element.QuerySelector("img")?.GetAttribute("src") ?? "";
                    var caption = PrepareTextContent(element.QuerySelector("div.thumbcaption")?.TextContent ?? "");
                    if (url.Length > 0 && caption.Length > 0)
                    {
                        sb.WriteLine($"=> {RewriteMediaUrl(url)} {caption}");
                        return;
                    }
                }
            }

            //a navigation note?
            if (element.GetAttribute("role") == "note" && element.ClassList.Contains("navigation-not-searchable"))
            {
                var lines = element.TextContent.Split(".").Where(x => x.Trim().Length > 0).ToArray();
                var tags = element.QuerySelectorAll("a");
                if (lines.Length == tags.Length)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (ShouldUseLink(tags[i]))
                        {
                            sb.WriteLine($"=> {ArticleUrl(tags[i].GetAttribute("title"))} {PrepareTextContent(lines[i])}.");
                            return;
                        }
                    }
                }
            }

            //is it a naked div?
            if (element.ClassList.Count() == 0 && element.ChildElementCount == 1)
            {
                sb.Write(RenderChildren(element));
                return;
            }
        }

        private void ProcessLi(HtmlElement element, TextWriter sb)
        {
            if (ListDepth == 1)
            {
                //if the entire item in a line, make it a link line,
                //otherwise its a bulleted list
                var links = element.QuerySelectorAll("a").ToList();
                if (links.Count > 0 && ShouldUseLink(links[0]) && element.TextContent.StartsWith(links[0].TextContent))
                {
                    sb.Write($"=> {ArticleUrl(links[0].GetAttribute("title"))} ");
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

        private void ProcessTable(HtmlElement element, TextWriter sb)
        {

            //ignore info boxes and message boxes
            if (element.ClassList.Contains("infobox"))
            {
                return;
            }

            if (element.GetAttribute("role") == "presentation")
            {
                if (element.ClassList.Contains("multicol"))
                {
                    var rows = element.QuerySelectorAll("tr").ToArray();
                    if (rows.Length == 1)
                    {
                        foreach (var col in rows[0].QuerySelectorAll("td"))
                        {
                            sb.Write(RenderChildren(col));
                        }
                        return;
                    }
                }
                return;
            }
            //sb.WriteLine("[Unable to render table]");
        }

        private string PrepareTextContent(IElement element)
            => PrepareTextContent(element.TextContent);

        private string PrepareTextContent(string s)
            => s.Trim().Replace("\n", "");

        private bool ShouldUseLink(IElement element)
            => element.HasAttribute("title") &&
                //ignore links to special pages!
                !element.GetAttribute("title").StartsWith("Special:") &&
                //links to pages that don't exist have a "new" class
                !element.ClassList.Contains("new");

    }
}
