using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Gemipedia.Models;

namespace Gemipedia.Renderer
{
    public class ArticleRenderer
    {
        ConverterSettings Settings;
        TextWriter Writer;
        ParsedPage Page;
        
        int sectionNum;

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
            Writer.WriteLine();
            Writer.WriteLine($"=> {CommonUtils.ReferencesUrl(Page.Title)} List of all {parsedPage.GetReferenceCount()} referenced articles");
            Writer.WriteLine($"=> {CommonUtils.PdfUrl(Page.EscapedTitle)} PDF of Article for Offline access");
            Writer.WriteLine($"=> {CommonUtils.WikipediaSourceUrl(Page.EscapedTitle)}  Original Source on Wikipedia");
        }

        private string RenderSection(Section section)
        {
            sectionNum++;

            StringBuilder sb = new StringBuilder();
            if (section.HasNavSuggestions)
            {
                //render navigation items at top
                foreach (var nav in section.NavSuggestions)
                {
                    sb.Append(nav.Render());
                }
                //add a blank link, since nav suggestion can be long
                sb.AppendLine();
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

            if(section.Links.HasLinks && !ShouldExcludeSectionIndex(section))
            {
                sb.AppendLine($"=> {CommonUtils.ReferencesUrl(Page.Title, sectionNum)} Section links: ({section.Links.Count} Articles)");
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

        private bool ShouldExcludeSectionIndex(Section section)
            => Settings.ArticleLinkSections.Contains(section.Title?.ToLower());

    }
}
