using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            RenderSections(sections);
        }

        private void RenderArticleTitle(string title)
        {
            Writer.WriteLine($"# {title}");
            Writer.WriteLine();
        }

        private void RenderSections(List<Section> sections)
        {
            foreach (var section in sections)
            {
                RenderSection(section);
            }
        }

        private void RenderSection(Section section)
        {
            //render the title
            RenderSectionTitle(section);

            //first render the content nodes for the section
            RenderContentNodes(section.ContentNodes);

            //render any subsections
            RenderSections(section.SubSections);
        }

        private void RenderSectionTitle(Section section)
        {
            if(section.IsSpecial)
            {
                return;
            }
            if (section.SectionDepth == 2)
            {
                Writer.WriteLine($"## {section.Title}");
            }
            else
            {
                //all other sections are at a level 3
                Writer.WriteLine($"### {section.Title}");
            }
        }

        private void RenderContentNodes(List<INode> nodes)
        {
            foreach(INode node in nodes)
            {
                Writer.Write(RenderContentNode(node));
            }
        }

        private string RenderContentNode(INode node, bool useLocalWriter = true)
        {
            StringWriter sb = new StringWriter();

            var children = node.ChildNodes.ToArray();
            for (int currIndex = 0; currIndex < children.Length; currIndex++)
            {
                var current = children[currIndex];
                switch (current.NodeType)
                {
                    case NodeType.Comment:
                        break;

                    case NodeType.Text:
                        sb.Write(current.TextContent);
                        break;

                    case NodeType.Element:
                        {
                            HtmlElement element = current as HtmlElement;
                            var nodeName = element.NodeName.ToLower();
                            switch (nodeName)
                            {

                                case "a":
                                    //RecordHyperlink(element);
                                    sb.Write(RenderContentNode(current));
                                    break;

                                case "blockquote":
                                    RenderContentNode(current).Trim().Split("\n").ToList()
                                        .ForEach(x => sb.WriteLine($">{x}"));
                                    sb.WriteLine();
                                    break;

                                case "br":
                                    sb.WriteLine();
                                    break;

                                case "dd":
                                    sb.Write("* ");
                                    sb.WriteLine(RenderContentNode(current));
                                    break;

                                case "dt":
                                    sb.Write(RenderContentNode(current));
                                    sb.WriteLine(":");
                                    break;

                                case "i":
                                    sb.Write('"');
                                    sb.Write(RenderContentNode(current));
                                    sb.Write('"');
                                    break;

                                case "u":
                                    sb.Write('_');
                                    sb.Write(RenderContentNode(current));
                                    sb.Write('_');
                                    break;

                                case "ol":
                                case "ul":
                                    ListDepth++;
                                    sb.Write(RenderContentNode(current));
                                    ListDepth--;
                                    break;

                                case "li":
                                    ProcessLi(element, sb);
                                    break;

                                case "p":
                                    {
                                        var innerContent = RenderContentNode(current).Trim();
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
                                    sb.Write(RenderContentNode(current));
                                    break;
                            }
                        }
                        break;
                    default:
                        throw new ApplicationException("Unhandled NODE TYPE!");
                }
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
                sb.Write(RenderContentNode(element));
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
                    sb.WriteLine(RenderContentNode(element).Trim());
                }
                else
                {
                    sb.Write("* ");
                    sb.WriteLine(RenderContentNode(element).Trim());
                }
            }
            else
            {
                sb.WriteLine();
                sb.Write("* * ");
                sb.WriteLine(RenderContentNode(element).Trim());
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
                            sb.Write(RenderContentNode(col));
                        }
                        return;
                    }
                }
                return;
            }
            sb.WriteLine("[Unable to render table]");
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
