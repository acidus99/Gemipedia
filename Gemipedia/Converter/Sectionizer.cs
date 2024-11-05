using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Gemipedia.Models;

namespace Gemipedia.Converter;

/// <summary>
/// Constructs a tree of sections and subsections for the Wiki content
/// </summary>
public class Sectionizer
{
    Stack<Section> SectionStack;

    ParsedPage ParsedPage;
    //grab once and cache
    string[] excludedSections = UserOptions.ExcludedSections;

    public ParsedPage ParseContent(string title, INode contentRoot)
    {
        ParsedPage = new ParsedPage
        {
            Title = title
        };

        SectionStack = new Stack<Section>();

        SectionStack.Push(new Section
        {
            IsSpecial = true,
            SectionDepth = 2,
        });

        var nodeList = contentRoot.ChildNodes.ToArray();

        for (int currIndex = 0, len = nodeList.Length; currIndex < len; currIndex++)
        {
            INode currNode = contentRoot.ChildNodes[currIndex];

            HeadingInfo? headingInfo = GetIfHeading(currNode);

            //is it a normal node
            if (headingInfo != null)
            {

                //we are supposed to skip this?
                if (ShouldSkipSection(headingInfo))
                {
                    currIndex = FastForward(currNode, nodeList, currIndex);
                    continue;
                }

                int depthOnStack = SectionStack.Peek().SectionDepth;
                //normalize to H2
                if (headingInfo.Level < 2)
                {
                    headingInfo.Level = 2;
                }
                if (headingInfo.Level > depthOnStack)
                {
                    //ok push a new section
                    PushNewSection(headingInfo);
                    continue;
                }
                else if (headingInfo.Level == depthOnStack)
                {
                    //pop the current section off
                    AddCompletedSection(SectionStack.Pop());
                    //push the new section
                    PushNewSection(headingInfo);
                }
                else
                {
                    //new section is
                    //found one lower!
                    //while the top of ths stacck is > the next one
                    while (SectionStack.Peek().SectionDepth > headingInfo.Level)
                    {
                        var tmpSection = SectionStack.Pop();
                        //add that as a subsection for the section of the top
                        SectionStack.Peek().SubSections.Add(tmpSection);
                    }
                    //pop the current section off
                    AddCompletedSection(SectionStack.Pop());
                    //push the new section
                    PushNewSection(headingInfo);
                }
            }
            else if (ShouldAddNode(currNode))
            {
                SectionStack.Peek().Nodes.Add(currNode);
            }
        }
        //combine remain stack
        while (SectionStack.Count > 0)
        {
            AddCompletedSection(SectionStack.Pop());
        }
        return ParsedPage;
    }

    private void AddCompletedSection(Section section)
    {
        //if there is still something on the stack, add it as a subsection
        if (SectionStack.Count > 0)
        {
            SectionStack.Peek().SubSections.Add(section);
        }
        else
        {
            ParsedPage.Sections.Add(section);
        }
    }

    private HeadingInfo? GetIfHeading(INode node)
    {

        if (node is not HtmlElement)
        {
            return null;
        }

        var htmlElement = node as HtmlElement;

        if (htmlElement.NodeName.Length == 2 &&
            htmlElement.NodeName[0] == 'H' &&
            char.IsDigit(htmlElement.NodeName[1]))
        {
            //traditional HTML used for a heading
            return new HeadingInfo
            {
                ID = htmlElement.QuerySelector("span.mw-headline").GetAttribute("id")?.ToLower() ?? "",
                Level = node.NodeName[1] - 48,
                Title = htmlElement.QuerySelector("span.mw-headline").TextContent.Trim().Replace("\n", "")
            };
        }
        //2024-07-21 : Sometime recently MediaWiki started output HTML with the header tags
        //wrapped in DIVs
        //TODO: I really should junk all this and operate on the WikiText directly...
        else if (htmlElement.NodeName == "DIV" &&
            htmlElement.ClassName != null &&
            htmlElement.ClassName.Contains("mw-heading") &&
            htmlElement.FirstElementChild != null &&
            htmlElement.FirstElementChild.NodeName.Length == 2 &&
            htmlElement.FirstElementChild.NodeName[0] == 'H' &&
            char.IsDigit(htmlElement.FirstElementChild.NodeName[1]))
        {
            //modern header

            return new HeadingInfo
            {
                ID = htmlElement.FirstElementChild.GetAttribute("id")?.ToLower() ?? "",
                Level = htmlElement.FirstElementChild.NodeName[1] - 48,
                Title = htmlElement.FirstElementChild.TextContent.Trim().Replace("\n", "")
            };
        }
        return null;
    }

    private void PushNewSection(HeadingInfo headingInfo)
        => SectionStack.Push(new Section
        {
            Title = headingInfo.Title,
            SectionDepth = headingInfo.Level
        });


    private bool ShouldAddNode(INode node)
    {
        switch (node.NodeType)
        {
            case NodeType.Text:
                if (node.TextContent.Trim().Length == 0)
                {
                    return false;
                }
                return true;

            case NodeType.Element:
                return true;

            default:
                return false;
        }
    }

    private bool ShouldSkipSection(HeadingInfo headingInfo)
        => excludedSections.Contains(headingInfo.ID);

    /// <summary>
    /// Fast forwards to the next element of the type as the provided element
    /// </summary>
    /// <param name="children"></param>
    /// <param name="currentIndex"></param>
    /// <returns></returns>
    private int FastForward(INode element, INode[] nodeList, int currentIndex)
    {
        int skipIndex = currentIndex + 1;
        //fast forward until we get to the next section
        for (; skipIndex < nodeList.Length; skipIndex++)
        {
            if ((nodeList[skipIndex].NodeType == element.NodeType) && (nodeList[skipIndex]).NodeName == element.NodeName)
            {
                break;
            }
        }
        return skipIndex - 1;
    }
}

internal class HeadingInfo
{
    public string Title { get; set; }
    public string ID { get; set; }
    public int Level { get; set; }
}