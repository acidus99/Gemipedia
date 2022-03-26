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
    public class Section
    {
        public List<INode> ContentNodes { get; set; } = new List<INode>();
        public List<Section> SubSections { get; set; }=  new List<Section>();

        //special sections don't have titles. the intro section is a special section
        public bool IsSpecial { get; set; } = false;

        public int SectionDepth { get; set; }
        public string Title { get; set; }

        public bool HasContent
            => ContentNodes.Count > 0 || SubSections.Count > 0;

    }
}
