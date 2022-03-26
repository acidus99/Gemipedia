using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace Gemipedia.Converter
{
    /// <summary>
    /// Constructs a tree of sections and subsections for the Wiki content
    /// </summary>
    public class Sectionizer
    {
        ConverterSettings Settings;

        Stack<Section> SectionStack;
        List<Section> OrderedSections;

        public Sectionizer(ConverterSettings settings)
        {
            Settings = settings;
        }

        public List<Section> ExtractSections(INode ContentRoot)
        {
            SectionStack = new Stack<Section>();
            OrderedSections = new List<Section>();

            SectionStack.Push(new Section
            {
                IsSpecial = true,
                SectionDepth = 2,
            });

            var nodeList = ContentRoot.ChildNodes.ToArray();

            for (int currIndex = 0, len = nodeList.Length; currIndex < len; currIndex++)
            {
                INode currNode = ContentRoot.ChildNodes[currIndex];
                //is it a normal node
                if(IsHeader(currNode))
                {
                    //we are supposed to skip this?
                    if(ShouldSkipSection(currNode))
                    {
                        currIndex = FastForward(currNode, nodeList, currIndex);
                        continue;
                    }

                    int depthOnStack = SectionStack.Peek().SectionDepth;
                    //what level is this?
                    int level = (int) (currNode.NodeName[1] - 48);
                    if(level > depthOnStack)
                    {
                        //ok push a new section
                        PushHeader(currNode, level);
                        continue;
                    }
                    else if(level == depthOnStack)
                    {
                        //pop the current section off
                        AddCompletedSection(SectionStack.Pop());
                        //push the new section
                        PushHeader(currNode, level);
                    }
                    else
                    {
                        //new section is
                        //found one lower!
                        //while the top of ths stacck is > the next one
                        while(SectionStack.Peek().SectionDepth > level)
                        {
                            var tmpSection = SectionStack.Pop();
                            //add that as a subsection for the section of the top
                            SectionStack.Peek().SubSections.Add(tmpSection);
                        }
                        //pop the current section off
                        AddCompletedSection(SectionStack.Pop());
                        //push the new section
                        PushHeader(currNode, level);
                    }
                } else if(ShouldAddNode(currNode))
                {
                    //add to the one on the queue
                    SectionStack.Peek().ContentNodes.Add(currNode);
                }
            }
            //combine remain stack
            while (SectionStack.Count > 0)
            {
                AddCompletedSection(SectionStack.Pop());
            }
            return OrderedSections;
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
                OrderedSections.Add(section);
            }
        }

        private void PushHeader(INode node, int level)
            => SectionStack.Push(new Section
            {
                Title = GetSectionText(node),
                SectionDepth = level
            });

        private bool IsHeader(INode node)
            => node.NodeType == NodeType.Element &&
                node.NodeName.Length == 2 &&
                node.NodeName[0] == 'H' &&
                char.IsDigit(node.NodeName[1]);

        private bool ShouldAddNode(INode node)
        {
            switch (node.NodeType)
            {
                case NodeType.Element:
                    return true;
                case NodeType.Text:
                    //not expecting to find real content here. All text content should be incased in other blocks
                    if (node.TextContent.Trim().Length > 0)
                    {
                        throw new ApplicationException($"Unexpected non-whitepsace text node in main content");
                    }
                    return false;
                default:
                    throw new ApplicationException($"Unexpected Node type '{node.NodeType}' in main content");
            }
        }

        private bool ShouldSkipSection(INode node)
        {
            var id = ((HtmlElement) node).QuerySelector("span.mw-headline").GetAttribute("id")?.ToLower() ?? "";
            return Settings.ExcludedSections.Contains(id);
        }

        /// <summary>
        /// Fast forwards to the next element of the type as the provided element
        /// </summary>
        /// <param name="children"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private int FastForward(INode element, INode [] nodeList, int currentIndex)
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

        private string GetSectionText(INode node)
            => ((HtmlElement) node).QuerySelector("span.mw-headline").TextContent.Trim().Replace("\n", "");
    }
}
