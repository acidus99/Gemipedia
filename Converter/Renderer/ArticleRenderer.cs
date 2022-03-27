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
            if (section.HasLinks)
            {
                if (!section.IsSpecial)
                {
                    Writer.WriteLine($"### {section.Title}");
                }
                foreach (var linkTitle in section.LinkedArticles)
                {
                    Writer.WriteLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
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

        private bool ShouldExcludeSectionIndex(Section section)
            => Settings.ArticleLinkSections.Contains(section.Title?.ToLower());

        private string RenderSection(Section section)
        {
            StringBuilder sb = new StringBuilder();

            //render navigation items at top
            foreach (SectionItem item in section.GetItems().Where(x=>x is NavSuggestionsItem))
            {
                sb.Append(item.Render());
            }

            //other content below, in order
            foreach (SectionItem item in section.GetItems().Where(x=> !(x is NavSuggestionsItem)))
            {
                sb.Append(item.Render());
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
