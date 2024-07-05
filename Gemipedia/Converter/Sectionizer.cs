using System;
using System.Collections.Generic;
using System.Linq;
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
            //is it a normal node
            if (IsHeader(currNode))
            {
                //we are supposed to skip this?
                if (ShouldSkipSection(currNode))
                {
                    currIndex = FastForward(currNode, nodeList, currIndex);
                    continue;
                }

                int depthOnStack = SectionStack.Peek().SectionDepth;
                //what level is this?
                int level = (int)(currNode.NodeName[1] - 48);
                //normalize to H2
                if (level < 2)
                {
                    level = 2;
                }
                if (level > depthOnStack)
                {
                    //ok push a new section
                    PushNewSection(currNode, level);
                    continue;
                }
                else if (level == depthOnStack)
                {
                    //pop the current section off
                    AddCompletedSection(SectionStack.Pop());
                    //push the new section
                    PushNewSection(currNode, level);
                }
                else
                {
                    //new section is
                    //found one lower!
                    //while the top of ths stacck is > the next one
                    while (SectionStack.Peek().SectionDepth > level)
                    {
                        var tmpSection = SectionStack.Pop();
                        //add that as a subsection for the section of the top
                        SectionStack.Peek().SubSections.Add(tmpSection);
                    }
                    //pop the current section off
                    AddCompletedSection(SectionStack.Pop());
                    //push the new section
                    PushNewSection(currNode, level);
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

    private bool IsHeader(INode node)
        => node.NodeType == NodeType.Element &&
            node.NodeName.Length == 2 &&
            node.NodeName[0] == 'H' &&
            char.IsDigit(node.NodeName[1]);

    private void PushNewSection(INode node, int level)
        => SectionStack.Push(new Section
        {
            Title = GetHeaderText(node),
            SectionDepth = level
        });

    public string GetHeaderText(INode node)
        => ((HtmlElement)node).QuerySelector("span.mw-headline").TextContent.Trim().Replace("\n", "");

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

    private bool ShouldSkipSection(INode node)
    {
        var id = ((HtmlElement)node).QuerySelector("span.mw-headline").GetAttribute("id")?.ToLower() ?? "";
        return excludedSections.Contains(id);
    }

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