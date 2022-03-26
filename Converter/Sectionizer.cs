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
    /// <summary>
    /// Constructs a tree of sections and subsections for the Wiki content
    /// </summary>
    public class Sectionizer
    {
        ConverterSettings Settings;

        public Sectionizer(ConverterSettings settings)
        {
            Settings = settings;
        }

        public List<Section> ExtractSections(INode ContentRoot)
        {
            //Make a temp section, containing all of our content nodes
            Section parentSection = new Section
            {
                IsSpecial = true,
                SectionDepth = 1,
                ContentNodes = ContentRoot.ChildNodes.ToList()
            };

            //parse that content into subsections
            ParseSection(parentSection);

            //The intro section doesn't actually have an explicit header
            //so its content is in the content nodes of the root.
            //pulls this out into an explicit section

            Section introSection = new Section
            {
                IsSpecial = true,
                SectionDepth = 2,
                ContentNodes = parentSection.ContentNodes
            };

            //return our list of sections, with the intro at the front
            var sectionList = parentSection.SubSections;
            sectionList.Insert(0, introSection);
            return sectionList;
        }

        /// <summary>
        /// Takes a section containing just a list of content nodes.
        /// Creates any subsections, updating the content nodes
        /// collection to just contain content for the section, and recursing in to look for more subsections
        /// </summary>
        /// <param name="section"></param>
        public void ParseSection(Section section)
        {
            //headers for subsection in this section
            string headerTag = $"H{section.SectionDepth + 1}";

            //buffer for content nodes that are in this section (e.g. not part of a subsection
            List<INode> sectionContentNodes = new List<INode>();

            //the current subsection we are processing, if any
            Section currSubSection = null;

            //iterate through the section nodes
            for (int currIndex = 0; currIndex < section.ContentNodes.Count; currIndex++)
            {
                INode currNode = section.ContentNodes[currIndex];
                if (currNode.NodeType == NodeType.Element && currNode.NodeName == headerTag)
                {
                    //we hit the start of the next section success!
                    if (currSubSection != null)
                    {
                        section.SubSections.Add(currSubSection);
                    }
                    currSubSection = null;

                    HtmlElement element = currNode as HtmlElement;

                    //should we skip this next section?
                    if (ShouldSkipSection(element))
                    {
                        //fast forward to the start of the next section
                        currIndex = FastForward(element, section.ContentNodes, currIndex);
                        continue;
                    }
                    //setup next section
                    currSubSection = new Section
                    {
                        Title = GetSectionText(element),
                        SectionDepth = section.SectionDepth + 1,
                    };
                }
                else if (ShouldAddNode(currNode))
                {
                    if (currSubSection == null)
                    {
                        sectionContentNodes.Add(currNode);
                    } else
                    {
                        currSubSection.ContentNodes.Add(currNode);
                    }
                }
            }

            //anything in the final buffer?
            if (currSubSection?.HasContent ?? false)
            {
                section.SubSections.Add(currSubSection);
            }
            //upload the content nodes for this section
            section.ContentNodes = sectionContentNodes;

            //parse any subsections
            //if my depth is 5, all subsections cannot have sections instead them
            if(section.SectionDepth < 5 && section.SubSections.Count > 0)
            {
                foreach(var subSection in section.SubSections)
                {
                    ParseSection(subSection);
                }
            } 
        }

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

        private bool ShouldSkipSection(HtmlElement element)
        {
            var id = element.QuerySelector("span.mw-headline").GetAttribute("id")?.ToLower() ?? "";
            return Settings.ExcludedSections.Contains(id);
        }

        /// <summary>
        /// Fast forwards to the next element of the type as the provided element
        /// </summary>
        /// <param name="children"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        private int FastForward(Element element, List<INode> nodeList, int currentIndex)
        {
            string nodeName = element.NodeName;
            int skipIndex = currentIndex + 1;
            //fast forward until we get to the next section
            for (; skipIndex < nodeList.Count; skipIndex++)
            {
                if ((nodeList[skipIndex] is IElement) && ((IElement)nodeList[skipIndex]).NodeName == nodeName)
                {
                    break;
                }
            }
            return skipIndex - 1;
        }

        private string GetSectionText(HtmlElement element)
            => element.QuerySelector("span.mw-headline").TextContent.Trim().Replace("\n", "");
    }
}
