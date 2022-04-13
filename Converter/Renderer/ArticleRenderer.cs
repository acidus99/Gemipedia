using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Renderer
{
    public class ArticleRenderer
    {
        ConverterSettings Settings;
        TextWriter Writer;
        ParsedPage Page;
        HashSet<string> alreadyUsedLinks = new HashSet<string>();

        public ArticleRenderer(ConverterSettings settings)
        {
            Settings = settings;
        }

        public void RenderArticle(ParsedPage parsedPage, TextWriter writer)
        {
            Writer = writer;
            Page = parsedPage;
            RenderArticleTitle();
            foreach(var section in parsedPage.Sections)
            {
                Writer.Write(RenderSection(section));
            }
            RenderIndex(parsedPage);
        }

        private void RenderArticleTitle()
        {
            Writer.WriteLine($"# {Page.Title}");
            int count = Page.GetAllImages().Count;
            if (count > 0)
            {
                Writer.WriteLine($"=> {CommonUtils.ImageGalleryUrl(Page.Title)} Gallery: {count} images");
            }

            Writer.WriteLine();
        }

        private void RenderIndex(ParsedPage parsedPage)
        {
            Writer.WriteLine();
            Writer.WriteLine("## Index of References");
            Writer.WriteLine("References to other articles, organized by section");
            foreach(var subSection in parsedPage.Sections.Where(x=>!ShouldExcludeSectionIndex(x)))
            {
                RenderIndexForSection(subSection);
            }
            Writer.WriteLine();
            Writer.WriteLine($"=> https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(parsedPage.Title)} Source on Wikipedia");
        }

        private void RenderIndexForSection(Section section)
        {
            //only display the section title if this section has links
            if (HasLinks(section))
            {
                if (!section.IsSpecial)
                {
                    Writer.WriteLine($"### {section.Title}");
                }
                foreach (var linkTitle in section.ArticleLinks.GetLinks())
                {
                    if(!alreadyUsedLinks.Contains(linkTitle))
                    {
                        alreadyUsedLinks.Add(linkTitle);
                        Writer.WriteLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
                    }
                }
            }
            if(section.HasSubSections)
            {
                foreach(var subSection in section.SubSections.Where(x => !ShouldExcludeSectionIndex(x)))
                {
                    RenderIndexForSection(subSection);
                }
            }
        }

        //do we have any links which have no already been rendered?
        private bool HasLinks(Section section)
            => section.ArticleLinks.HasLinks &&
                section.ArticleLinks.GetLinks()
                .Where(title => !alreadyUsedLinks.Contains(title)).FirstOrDefault() != null;

        private bool ShouldExcludeSectionIndex(Section section)
            => Settings.ArticleLinkSections.Contains(section.Title?.ToLower());

        private string RenderSection(Section section)
        {
            StringBuilder sb = new StringBuilder();

            //render navigation items at top
            foreach (var nav in section.NavSuggestions)
            {
                sb.Append(nav.Render());
            }

            //other content below, in order
            foreach (SectionItem item in section.GeneralContent)
            {
                //skip media items if configured to do so
                if(!Settings.ShouldConvertMedia && item is MediaItem)
                {
                    continue;
                }
                sb.Append(item.Render());
            }

            foreach (var infoBox in section.Infoboxes)
            {
                //skip media items if configured to do so
                sb.Append(infoBox.Render());
            }

            foreach (var subSection in section.SubSections)
            {
                sb.Append(RenderSection(subSection));
            }

            //if a section has no content, don't write anything
            if(sb.Length == 0)
            {
                return "";
            }

            StringBuilder completeSection = new StringBuilder();
            if (!section.IsSpecial)
            {
                if (section.SectionDepth == 2)
                {
                    completeSection.AppendLine($"## {section.Title}");
                }
                else
                {
                    //all other sections are at a level 3
                    completeSection.AppendLine($"### {section.Title}");
                }
            }
            completeSection.Append(sb.ToString());
            return completeSection.ToString();
        }

    }
}
