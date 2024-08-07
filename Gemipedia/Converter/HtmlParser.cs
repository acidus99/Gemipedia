﻿using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Gemipedia.Converter.Filter;
using Gemipedia.Converter.Special;
using Gemipedia.Models;

namespace Gemipedia.Converter;

/// <summary>
/// parses HTML nodes into Section Items
/// </summary>
public class HtmlParser
{
    private static readonly string[] blockElements = new string[] { "address", "article", "aside", "blockquote", "canvas", "dd", "div", "dl", "dt", "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "li", "main", "nav", "noscript", "ol", "p", "pre", "section", "table", "tfoot", "ul", "video" };

    private List<SectionItem> items = new List<SectionItem>();

    private int listDepth = 0;

    Buffer buffer = new Buffer();

    private bool inPreformatted = false;
    private bool inMathformula = false;

    public bool HasGeminiFormatting { get; private set; } = false;

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

    private void AddItems(IEnumerable<SectionItem> newItems)
    {
        if (newItems?.Count() > 0)
        {
            FlushBuffer();
            items.AddRange(newItems);
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
                if (buffer.AtLineStart)
                {
                    buffer.Append(textNode.TextContent.TrimStart());
                }
                else
                {
                    buffer.Append(textNode.TextContent);
                }
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
                ProcessAnchor(element);
                break;

            case "blockquote":
                HasGeminiFormatting = true;
                buffer.EnsureAtLineStart();
                buffer.InBlockquote = true;
                ParseChildern(element);
                buffer.InBlockquote = false;
                break;

            case "br":
                buffer.AppendLine();
                break;

            case "dd":
                HasGeminiFormatting = true;
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

            case "figure":
                ProcessFigure(element);
                break;

            case "i":
                if (ShouldUseItalics(element))
                {
                    buffer.Append("\"");
                    ParseChildern(element);
                    buffer.Append("\"");
                }
                else
                {
                    ParseChildern(element);
                }
                break;

            case "li":
                ProcessLi(element);
                break;

            case "ol":
                ProcessList(element);
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
                HasGeminiFormatting = true;
                buffer.EnsureAtLineStart();
                buffer.AppendLine("```");
                inPreformatted = true;
                ParseChildern(element);
                buffer.EnsureAtLineStart();
                inPreformatted = false;
                buffer.AppendLine("```");
                break;

            case "sub":
                ProcessSub(element);
                break;

            case "sup":
                ProcessSup(element);
                break;

            case "table":
                ProcessTable(element);
                break;

            case "u":
                buffer.Append("_");
                ParseChildern(element);
                buffer.Append("_");
                break;

            case "ul":
                ProcessUl(element);
                break;

            default:
                ProcessGenericTag(element);
                break;
        }
    }

    public static bool ShouldProcessElement(HtmlElement element,string normalizedTagName)
    {
        //A MathElement is of type element, but it not an HtmlElement
        //so it will be null
        if (element == null)
        {
            return false;
        }

        if(element.ClassName?.Contains("navigation") ?? false)
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

    //should we use apply italic formatting around this element?
    private bool ShouldUseItalics(HtmlElement element)
    {
        //if we are already inside a math formula, don't do italics
        if(inMathformula)
        {
            return false;
        }
        var siblingTag = element.NextElementSibling?.NodeName?.ToLower() ?? "";
        if(siblingTag == "sub" || siblingTag == "sup")
        {
            return false;
        }
        return true;
    }

    private static bool IsInvisible(HtmlElement element)
       => element.GetAttribute("style")?.Contains("display:none") ?? false;

    private void ProcessAnchor(HtmlElement anchor)
    {
        if (GeoParser.IsGeoLink(anchor))
        {
            AddItem(GeoParser.ParseGeo(anchor));
        }
        else if (IsWikiDataLink(anchor))
        {
            //we don't want to process the children if this links to Wikidata
            return;
        }
        else
        {
            buffer.Links.Add(anchor);
        }
        ParseChildern(anchor);
    } 

    private void ProcessDiv(HtmlElement div)
    {
        // Is this a legacy media div, that is also not a location map
        // https://www.mediawiki.org/wiki/Parsoid/Parser_Unification/Media_structure/FAQ
        // is it a media div?
        if (div.ClassList.Contains("thumb") && !div.ClassList.Contains("locmap"))
        {
            if (div.ClassList.Contains("tmulti"))
            {
                AddItems(MediaParser.ConvertMontage(div, div.QuerySelector(".thumbcaption")));
                return;
            }
            AddItem(MediaParser.ConvertMedia(div, div.QuerySelector(".thumbcaption")));
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

    private void ProcessFigure(HtmlElement figure)
    {
        //Support the new markup output for images
        //see: https://www.mediawiki.org/wiki/Parsoid/Parser_Unification/Media_structure/FAQ
        if (figure.GetAttribute("typeof") == "mw:File/Thumb")
        {
            AddItem(MediaParser.ConvertMedia(figure, figure.QuerySelector("figcaption")));
        }
        return;
    }

    private void ProcessGenericTag(HtmlElement element)
    {
        //is this a math element?
        if(element.ClassList.Contains("mwe-math-element"))
        {
            HasGeminiFormatting = true;
            //math elements have to be displayed at the start of the like
            buffer.EnsureAtLineStart();
            buffer.AppendLine(MathConverter.ConvertMath(element));
            return;
        } 

        if(element.ClassList.Contains("texhtml") && !inMathformula)
        {
            inMathformula = true;
            buffer.Append("\"");
            ParseChildern(element);
            buffer.Append("\"");
            inMathformula = false;
            return;
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
            HasGeminiFormatting = true;
            buffer.EnsureAtLineStart();
            buffer.SetLineStart("* ");
            ParseChildern(li);
            buffer.EnsureAtLineStart();
        }
        else
        {
            HasGeminiFormatting = true;
            buffer.EnsureAtLineStart();
            buffer.SetLineStart("* * ");
            ParseChildern(li);
            buffer.EnsureAtLineStart();
        }
    }

    private void ProcessList(HtmlElement element)
    {
        //block element
        buffer.EnsureAtLineStart();
        listDepth++;
        ParseChildern(element);
        listDepth--;
        buffer.EnsureAtLineStart();
    }

    private void ProcessSub(HtmlElement element)
    {
        var textExtractor = new TextExtractor
        {
            ShouldCollapseNewlines = true,
            ShouldConvertImages = false,
        };
        textExtractor.Extract(element);

        var content = textExtractor.Content.Trim();
        if (content.Length > 0) {
            var subConverter = new SubscriptConverter();
            if (subConverter.Convert(content))
            {
                //we successfully converted everything
                buffer.Append(subConverter.Converted);
            }
            //couldn't convert, fall back to using ⌄ ...
            else if (content.Length == 1)
            {
                buffer.Append("˅");
                buffer.Append(content);
            }
            else
            {
                buffer.Append("˅(");
                buffer.Append(content);
                buffer.Append(")");
            }
            buffer.Links.Add(textExtractor);
        }
    }

    private void ProcessSup(HtmlElement element)
    {
        var textExtractor = new TextExtractor
        {
            ShouldCollapseNewlines = true,
            ShouldConvertImages = false,
        };
        textExtractor.Extract(element);
        var content = textExtractor.Content.Trim();

        if (content.Length > 0)
        {
            var supConverter = new SuperscriptConverter();
            if (supConverter.Convert(content))
            {
                //we successfully converted everything
                buffer.Append(supConverter.Converted);
            }
            //couldn't convert, fall back to using ^...
            else if (content.Length == 1)
            {
                buffer.Append("^");
                buffer.Append(content);
            }
            else
            {
                buffer.Append("^(");
                buffer.Append(content);
                buffer.Append(")");
            }
            buffer.Links.Add(textExtractor);
        }
    }

    private void ProcessTable(HtmlElement table)
    {
        if (InfoboxParser.IsInfobox(table))
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
        HasGeminiFormatting = true;
        //treat everying like a table?
        AddItem(WikiTableConverter.ConvertWikiTable(table));
    }

    private void ProcessUl(HtmlElement ul)
    {
        //gallery?
        if (ul.ClassList.Contains("gallery"))
        {
            AddItems(MediaParser.ConvertGallery(ul));
            return;
        }

        ProcessList(ul);
    }

    public static bool ShouldDisplayAsBlock(HtmlElement element)
    {
        var nodeName = element.NodeName.ToLower();
        if (!blockElements.Contains(nodeName))
        {
            return false;
        }
        //its a block, display it as inline?
        return !IsInline(element);
    }

    private static bool IsInline(HtmlElement element)
        => element.GetAttribute("style")?.Contains("display:inline") ?? false;

    private bool IsMulticolumnLayoutTable(HtmlElement element)
        => element.GetAttribute("role") == "presentation" &&
            element.ClassList.Contains("multicol") &&
            element.HasChildNodes &&
            element.Children[0].NodeName == "TBODY" &&
            element.Children[0].HasChildNodes &&
            element.Children[0].Children[0].NodeName == "TR";

    //does an anchor point to Wikidata?
    private bool IsWikiDataLink(HtmlElement element)
        => element.GetAttribute("href")?.Contains("//www.wikidata.org/") ?? false;

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
                HasGeminiFormatting = true;
                buffer.EnsureAtLineStart();
                buffer.SetLineStart($"=> {RouteOptions.ArticleUrl(links[0].GetAttribute("title"))} ");
                ParseChildern(li);
                buffer.EnsureAtLineStart();
                return true;
            }
        }
        return false;
    }
}
