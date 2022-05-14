//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using AngleSharp;
//using AngleSharp.Html.Parser;
//using AngleSharp.Html.Dom;
//using AngleSharp.Dom;
//using System.Text;

//using Gemipedia.Converter.Models;

//namespace Gemipedia.Converter.Parser
//{
//	public class MegaConverter
//	{
//		public MegaConverter()
//		{
//		}


//        private ArticleLinkCollection articleLinks;
//        private StringBuilder textBuffer;
//        private List<SectionItem> items;
//        private ImageBlockParser imageParser;

//        public MegaConverter()
//        {
//            items = new List<SectionItem>();
//            imageParser = new ImageBlockParser();
//            textBuffer = new StringBuilder();
//        }

//        private void AddItem(SectionItem item)
//        {
//            if (item != null)
//            {
//                items.Add(item);
//            }
//        }

//        public IEnumerable<SectionItem> ProcessNode(INode node)
//        {
//            items.Clear();
//            textBuffer.Clear();
//            articleLinks = new ArticleLinkCollection();
//            ConvertNode(node);
//            return items;
//        }

//        private string ConvertChildren(INode node, bool preserveWhitespaceText = false)
//            => ConvertNodes(node.ChildNodes, preserveWhitespaceText);

//        private string ConvertNodes(IEnumerable<INode> nodes, bool preserveWhitespaceText)
//        {
//            foreach (var node in nodes)
//            {
//                ConvertNode(node);
//            }
//        }

//        private void ConvertNode(INode current, bool preserveWhitespaceText = false)
//        {
//            switch (current.NodeType)
//            {
//                case NodeType.Comment:
//                    break;

//                case NodeType.Text:
//                    if (preserveWhitespaceText)
//                    {
//                        textBuffer.Append(current.TextContent);
//                    }
//                    else
//                    {
//                        //if its not only whitespace add it.
//                        if (current.TextContent.Trim().Length > 0)
//                        {
//                            textBuffer.Append(current.TextContent);
//                        }
//                        //if its whitepsace, but doesn't have a newline
//                        else if (!current.TextContent.Contains('\n'))
//                        {
//                            textBuffer.Append(current.TextContent);
//                        }
//                    }
//                    break;

//                case NodeType.Element:
//                    {
//                        HtmlElement element = current as HtmlElement;
//                        if(ShouldSkipElement(element))
//                        {
//                            break;
//                        }

//                        var nodeName = element.NodeName.ToLower();
//                        switch (nodeName)
//                        {

//                            case "a":
//                                //record the link
//                                RecordLink(element);
//                                articleLinks.Add(element);
//                                ConvertChildren(current, preserveWhitespaceText));
//                                break;

//                            case "blockquote":
//                                RenderChildren(current, preserveWhitespaceText).Trim().Split("\n").ToList()
//                                    .ForEach(x => sb.WriteLine($">{x}"));
//                                sb.WriteLine();
//                                break;

//                            case "br":
//                                sb.WriteLine();
//                                break;

//                            case "dd":
//                                sb.Write("* ");
//                                sb.WriteLine(RenderChildren(current, preserveWhitespaceText));
//                                break;

//                            case "dt":
//                                sb.Write(RenderChildren(current, preserveWhitespaceText));
//                                sb.WriteLine(":");
//                                break;

//                            //we still have to handle headers here, since headers inside
//                            //of other complex markup, (like tables) will end up being
//                            //rendered here

//                            case "h1":
//                                sb.WriteLine($"# {CommonUtils.GetHeaderText(element)}");
//                                break;

//                            case "h2":
//                                sb.WriteLine($"## {CommonUtils.GetHeaderText(element)}");
//                                break;

//                            case "h3":
//                            case "h4":
//                            case "h5":
//                            case "h6":
//                                sb.WriteLine($"### {CommonUtils.GetHeaderText(element)}");
//                                break;

//                            case "i":
//                                sb.Write('"');
//                                sb.Write(RenderChildren(current, preserveWhitespaceText));
//                                sb.Write('"');
//                                break;

//                            case "li":
//                                ProcessLi(element, sb);
//                                break;

//                            case "ol":
//                            case "ul":
//                                ListDepth++;
//                                sb.Write(RenderChildren(current, preserveWhitespaceText));
//                                ListDepth--;
//                                break;

//                            case "p":
//                                {
//                                    var innerContent = RenderChildren(current, preserveWhitespaceText).Trim();
//                                    if (innerContent.Length > 0)
//                                    {
//                                        sb.WriteLine(innerContent);
//                                        sb.WriteLine();
//                                    }
//                                }
//                                break;

//                            case "pre":
//                                sb.WriteLine("```");
//                                sb.WriteLine(RenderChildren(current, true));
//                                sb.WriteLine("```");
//                                break;

//                            case "span":
//                                {
//                                    if (!ShouldSkipSpan(element))
//                                    {
//                                        var special = ConvertSpan(element, sb);
//                                        if (special.Length > 0)
//                                        {
//                                            sb.Write(special);
//                                        }
//                                        else
//                                        {
//                                            sb.Write(RenderChildren(current, preserveWhitespaceText));
//                                        }
//                                    }
//                                }
//                                break;

//                            case "sup":
//                                {
//                                    var supscript = RenderChildren(current, false).Trim();
//                                    if (supscript.Length > 1)
//                                    {
//                                        sb.Write("^(");
//                                        sb.Write(supscript);
//                                        sb.Write(")");
//                                    }
//                                    else
//                                    {
//                                        sb.Write("^");
//                                        sb.Write(supscript);
//                                    }
//                                }
//                                break;

//                            case "u":
//                                sb.Write('_');
//                                sb.Write(RenderChildren(current, preserveWhitespaceText));
//                                sb.Write('_');
//                                break;

//                            //tags to ignore
//                            case "link":
//                            case "style":
//                                break;

//                            default:
//                                sb.Write(RenderChildren(current, preserveWhitespaceText));
//                                break;
//                        }
//                    }
//                    break;
//                default:
//                    throw new ApplicationException("Unhandled NODE TYPE!");
//            }

//            return sb.ToString();
//        }

//        private void RecordLink(HtmlElement element)
//        {

//        }

//        private bool ShouldSkipElement(HtmlElement element)
//            =>  //A MathElement is of type element, but it not an HtmlElement
//                (element == null) ||
//                //if its supposed to be invisible, we should not process it
//                (IsInvisible(element));

//        private bool IsInvisible(HtmlElement element)
//            => element.GetAttribute("style")?.Contains("display:none") ?? false;

//    }
//}

