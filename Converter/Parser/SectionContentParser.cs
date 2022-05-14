using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;
using System.Text;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser
{
    /// <summary>
    /// Parses html content inside a section and converts it into
    /// SectionItems which can rearranged and then rendered
    /// </summary>
    public class SectionContentParser
    {

        private List<SectionItem> items;
        private ImageBlockParser imageParser;

        public SectionContentParser()
        {
            items = new List<SectionItem>();
            imageParser = new ImageBlockParser();
        }

        private void AddItem(SectionItem item)
        {
            if(item != null)
            {
                items.Add(item);
            }
        }

        public IEnumerable<SectionItem> ParseElement(HtmlElement element)
        {
            items.Clear();
            ParseElementHelper(element);
            return items;
        }

        private void ParseElementsHelper(IHtmlCollection<IElement> elements)
            => elements.ToList().ForEach(x => ParseElementHelper(x as HtmlElement));

        private void ParseElementHelper(HtmlElement element)
        {

            switch (element.NodeName.ToLower())
            {
                case "div":
                    ParseDiv(element);
                    break;

                //high level content blocks we are ignoring
                case "table":
                    ParseTable(element);
                    break;

                default:
                    ParseHtmlElement(element);
                    break;
            }
        }

        private void ParseDiv(HtmlElement element)
        {
            //is it a media div?
            if (element.ClassList.Contains("thumb") && !element.ClassList.Contains("locmap"))
            {
                AddItem(imageParser.Convert(element, element.QuerySelector(".thumbcaption")));
                return;
            }

            //a navigation note?
            if (element.GetAttribute("role") == "note" && element.ClassList.Contains("navigation-not-searchable"))
            {
                AddItem(SpecialBlockConverter.ConvertNavigationNotes(element));
                return;
            }

            //is it a naked with a table?
            if (element.ClassList.Count() == 0 &&
                element.ChildElementCount == 1 &&
                element.FirstElementChild.NodeName == "TABLE")
            {
                ParseTable(element.FirstElementChild as HtmlElement);
                return;
            }

            if(element.ClassList.Contains("timeline-wrapper"))
            {
                AddItem(SpecialBlockConverter.ConvertTimeline(element));
                return;
            }

            //A Div we can just pass through?
            //e.g. highlighted pre-formatted text?
            //or columnized unsorted list
            if (element.ClassList.Contains("mw-highlight") ||
                element.ClassList.Contains("div-col"))
            {
                ParseHtmlElement(element);
                return;
            }
        }

        private void ParseHtmlElement(HtmlElement element)
        {
            HtmlTranslater translater = new HtmlTranslater();

            var contents = translater.RenderGemtext(element);
            if (contents.Length > 0)
            {
                AddItem(new ContentItem
                {
                    Content = contents,
                    Links = translater.Links
                });
            }
        }

        private void ParseTable(HtmlElement table)
        {
            //is it a data table?
            if (table.ClassList.Contains("wikitable"))
            {
                AddItem(SpecialBlockConverter.ConvertWikiTable(table));
                return;
            }

            //is it a table just used to create a multicolumn view?
            if (IsMulticolumnLayoutTable(table))
            {
                ParseMulticolmnTable(table);
                return;
            }

            if(table.ClassList.Contains("infobox"))
            {
                InfoboxParser parser = new InfoboxParser();
                AddItem(parser.Parse(table));
                return;
            }
        }

        private bool IsMulticolumnLayoutTable(HtmlElement element)
            => element.GetAttribute("role") == "presentation" &&
                element.ClassList.Contains("multicol") &&
                element.HasChildNodes &&
                element.Children[0].NodeName == "TBODY" &&
                element.Children[0].HasChildNodes &&
                element.Children[0].Children[0].NodeName == "TR";

        private void ParseMulticolmnTable(HtmlElement table)
        {
            table.Children[0].Children[0].Children
                .Where(x => x.NodeName == "TD").ToList()
                .ForEach(x=> ParseElementsHelper(x.Children));
        }
    }
}
