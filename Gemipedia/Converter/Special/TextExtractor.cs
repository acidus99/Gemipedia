﻿using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter.Special;

/// <summary>
/// Extracts text
/// </summary>
public class TextExtractor : ITextContent
{
    public string Content
        => ShouldCollapseNewlines ?
                    CollapseNewlines(buffer.Content) :
                    buffer.Content;

    public ArticleLinkCollection Links
        => buffer.Links;

    public bool ShouldCollapseNewlines { get; set; } = false;
    public bool ShouldConvertImages { get; set; } = false;

    //sets the character we use for newline replacement
    public string NewlineReplacement { get; set; } = " ";

    private static readonly Regex whitespace = new Regex(@"\s+", RegexOptions.Compiled);

    private Buffer buffer = new Buffer();

    public void Extract(params INode[] nodes)
        => Extract(nodes.Where(x => x != null).FirstOrDefault());

    public void Extract(INode current)
    {
        buffer.Reset();
        if (current == null)
        {
            //nothing to do
            return;
        }
        ExtractInnerTextHelper(current);
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
                    var nodeName = element?.NodeName.ToLower();

                    if (!HtmlParser.ShouldProcessElement(element, nodeName))
                    {
                        return;
                    }

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
                            if (ShouldConvertImages)
                            {
                                buffer.Append(ConvertImage(element));
                            }
                            break;

                        default:
                            if (HtmlParser.ShouldDisplayAsBlock(element))
                            {
                                buffer.EnsureAtLineStart();
                                ExtractChildrenText(current);
                                buffer.EnsureAtLineStart();
                            }
                            else
                            {
                                ExtractChildrenText(current);
                            }
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
        => s.Replace("\n", NewlineReplacement).Trim();

    private string CollapseSpaces(string s)
        => whitespace.Replace(s, " ");

    private string ConvertImage(HtmlElement element)
    {
        var alt = element.GetAttribute("alt");
        if (string.IsNullOrEmpty(alt))
        {
            alt = element.GetAttribute("title");
        }
        return !string.IsNullOrEmpty(alt) ?
            $"[Image: {alt}] " :
            "";
    }
}