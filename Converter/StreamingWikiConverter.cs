using System;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using System.Text;

namespace WikiProxy.Converter
{
    public class StreamingWikiConverter
    {
        private TextWriter Writer;

        private const string ProxyMediaUrl = "/cgi-bin/wp.cgi/media/thumb.jpg";
        private const string ViewUrl = "/cgi-bin/wp.cgi/view";

        public string Title { get; set; }

        public string GemText { get; set; }

        IHtmlDocument document;
        LinkArticles linkedArticles;

        int ListDepth;

        string currentSection = "intro";

        private string[] sectionsToExclude = { "bibliography", "citations", "external_links", "notes", "references" };

        public StreamingWikiConverter(TextWriter writer)
        {
            linkedArticles = new LinkArticles();
            Writer = writer;
            ListDepth = 0;
        }

        public void ParseHtml(string title, string wikiHtml)
        {
            Title = title;
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();

            document = parser.ParseDocument(wikiHtml);

            //Phase 1: Bulk Remove tags
            RemoveTags();
            var root = document.QuerySelector("div.mw-parser-output");

            Writer.WriteLine($"# {Title}");
            ProcessNode(root, false);
            Writer.WriteLine();
            Writer.WriteLine("## Index of References");
            foreach(var linkTitle in linkedArticles.GetLinks())
            {
                Writer.WriteLine($"=> {ArticleUrl(linkTitle)} {linkTitle}");
            }
            Writer.WriteLine();
            Writer.WriteLine($"=> https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(Title)} View '{title}' on Wikipedia");
            Writer.Flush();
        }

        private string ProcessNode(INode node, bool useLocalWriter = true)
        {
            TextWriter sb = (useLocalWriter) ? new StringWriter() : Writer;

            var children = node.ChildNodes.ToArray();
            for (int currIndex = 0; currIndex < children.Length; currIndex++)
            {
                var current = children[currIndex];
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
                                    RecordHyperlink(element);
                                    sb.Write(ProcessNode(current));
                                    break;

                                case "blockquote":
                                    ProcessNode(current).Trim().Split("\n").ToList()
                                        .ForEach(x => sb.WriteLine($">{x}"));
                                    sb.WriteLine();
                                    break;

                                case "br":
                                    sb.WriteLine();
                                    break;

                                case "dd":
                                    sb.Write("* ");
                                    sb.WriteLine(ProcessNode(current));
                                    break;

                                case "dt":
                                    sb.Write(ProcessNode(current));
                                    sb.WriteLine(":");
                                    break;

                                case "h1":
                                    if (ShouldBeSkipped(element))
                                    {
                                        currIndex = FastForward(element, children, currIndex);
                                    }
                                    else
                                    {
                                        sb.WriteLine($"# {GetHeaderText(element)}");
                                    }
                                    break;

                                case "h2":
                                    if (ShouldBeSkipped(element))
                                    {
                                        currIndex = FastForward(element, children, currIndex);
                                    }
                                    else
                                    {
                                        sb.WriteLine($"## {GetHeaderText(element)}");
                                    }
                                    break;

                                case "h3":
                                case "h4":
                                case "h5":
                                case "h6":
                                    if (ShouldBeSkipped(element))
                                    {
                                        currIndex = FastForward(element, children, currIndex);
                                    }
                                    else
                                    {
                                        sb.WriteLine($"### {GetHeaderText(element)}");
                                    }
                                    break;


                                case "i":
                                    sb.Write('"');
                                    sb.Write(ProcessNode(current));
                                    sb.Write('"');
                                    break;

                                case "u":
                                    sb.Write('_');
                                    sb.Write(ProcessNode(current));
                                    sb.Write('_');
                                    break;

                                case "ol":
                                case "ul":
                                    ListDepth++;
                                    sb.Write(ProcessNode(current));
                                    ListDepth--;
                                    break;

                                case "li":
                                    ProcessLi(element, sb);
                                    break;

                                case "p":
                                    {
                                        var innerContent = ProcessNode(current).Trim();
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
                                    sb.Write(ProcessNode(current));
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
            => ProxyMediaUrl + "?" + WebUtility.UrlEncode(url);

        private string ArticleUrl(string title)
            => ViewUrl + "?" + WebUtility.UrlEncode(title);

        /// <summary>
        /// Fast forwards to the next element of the type as the current element
        /// </summary>
        /// <param name="children"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private int FastForward(Element element, INode [] children, int currentIndex)
        {
            string nodeName = element.NodeName;
            int skipIndex = currentIndex + 1;
            //fast forward until we get to the next section
            for(;skipIndex < children.Length; skipIndex++)
            {
                if((children[skipIndex] is IElement) && ((IElement)children[skipIndex]).NodeName == nodeName)
                {
                    break;
                }
            }
            return skipIndex - 1;
        }

        private void RecordHyperlink(HtmlElement element)
        {
            if (ShouldUseLink(element))
            {
                linkedArticles.AddLink(element.GetAttribute("title"));
            }
        }

        private bool ShouldUseLink(IElement element)
            => element.HasAttribute("title") &&
                //ignore links to special pages!
                !element.GetAttribute("title").StartsWith("Special:") &&
                //links to pages that don't exist have a "new" class
                !element.ClassList.Contains("new");

        private void ProcessDiv(HtmlElement element, TextWriter sb)
        {
            //is it a picture?
            if(element.ClassList.Contains("thumb") && !element.ClassList.Contains("locmap"))
            {
                var url = element.QuerySelector("img")?.GetAttribute("src") ?? "";
                var caption = PrepareTextContent(element.QuerySelector("div.thumbcaption")?.TextContent ?? "");
                if(url.Length > 0 && caption.Length > 0)
                {
                    sb.WriteLine($"=> {RewriteMediaUrl(url)} {caption}");
                }
            }

            //a navigation note?
            if(element.GetAttribute("role") == "note" && element.ClassList.Contains("navigation-not-searchable"))
            {
                var lines = element.TextContent.Split(".").Where(x=>x.Trim().Length > 0).ToArray();
                var tags =element.QuerySelectorAll("a");
                if(lines.Length == tags.Length)
                {
                    for(int i=0; i< lines.Length; i++)
                    {
                        if (ShouldUseLink(tags[i]))
                        {
                            sb.WriteLine($"=> {ArticleUrl(tags[i].GetAttribute("title"))} {PrepareTextContent(lines[i])}.");
                        }
                    }
                }
            }

            //is it a naked div?
            if(element.ClassList.Count() == 0 && element.ChildElementCount == 1)
            {
                sb.Write(ProcessNode(element));
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
                    sb.WriteLine(ProcessNode(element).Trim());
                }
                else
                {
                    sb.Write("* ");
                    sb.WriteLine(ProcessNode(element).Trim());
                }
            } else
            {
                sb.WriteLine();
                sb.Write("* * ");
                sb.WriteLine(ProcessNode(element).Trim());
            }
        }

        private void ProcessTable(HtmlElement element, TextWriter sb)
        {

            //ignore info boxes and message boxes
            if (element.ClassList.Contains("infobox") || element.ClassList.Contains("mbox-small"))
            {
                return;
            }

            if (element.GetAttribute("role") == "presentation" && element.ClassList.Contains("multicol"))
            {
                var rows = element.QuerySelectorAll("tr").ToArray();
                if(rows.Length == 1)
                {
                    foreach(var col in rows[0].QuerySelectorAll("td"))
                    {
                        sb.Write(ProcessNode(col));
                    }
                    return;
                }
            }
            sb.WriteLine("[Unable to render table]");
        }

        private bool ShouldBeSkipped(HtmlElement element)
        {
            var id = element.QuerySelector("span.mw-headline").GetAttribute("id")?.ToLower() ?? "";
            return sectionsToExclude.Contains(id);
        }

        private string GetHeaderText(HtmlElement element)
            => PrepareTextContent(element.QuerySelector("span.mw-headline"));

        private string PrepareTextContent(IElement element)
            => PrepareTextContent(element.TextContent);

        private string PrepareTextContent(string s)
            => s.Trim().Replace("\n", "");

        private void RemoveTags()
        {
            //all <sup> tags are used to link to references. 
            document.QuerySelectorAll("sup").ToList().ForEach(x => x.Remove());
            //all span holders for flag icons
            document.QuerySelectorAll("span.flagicon").ToList().ForEach(x => x.Remove());
        }
    }
}
