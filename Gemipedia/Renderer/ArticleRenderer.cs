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
        int sectionID;

        public ArticleRenderer(ConverterSettings settings)
        {
            Settings = settings;
            sectionID = 0;
        }

        public void RenderArticle(ParsedPage parsedPage, TextWriter writer)
        {
            Writer = writer;
            Page = parsedPage;

            RenderArticleHeader();
            foreach(var section in parsedPage.Sections)
            {
                Writer.Write(RenderSection(section));
            }
            RenderArticleFooter(parsedPage);
        }

        private void RenderArticleHeader()
        {
            Writer.WriteLine($"# {Page.Title}");
            int count = Page.GetAllImages().Count;
            if (count > 0)
            {
                Writer.WriteLine($"=> {CommonUtils.ImageGalleryUrl(Page.Title)} Gallery: {count} images");
            }
            //TODO: Geo here!
            Writer.WriteLine($"=> {CommonUtils.SearchUrl(Page.Title)} Other articles that mention '{Page.Title}'");
            Writer.WriteLine();
        }

        private void RenderArticleFooter(ParsedPage parsedPage)
        {
            Writer.WriteLine();
            Writer.WriteLine("## Article Resources");
            Writer.WriteLine($"=> {CommonUtils.ReferencesUrl(Page.Title)} List of all {parsedPage.GetReferenceCount()} referenced articles");
            Writer.WriteLine($"=> {CommonUtils.SearchUrl(Page.Title)} Search for articles that mention '{Page.Title}'");
            Writer.WriteLine($"=> {CommonUtils.PdfUrl(Page.EscapedTitle)} Download article PDF for offline access");
            Writer.WriteLine($"=> {CommonUtils.WikipediaSourceUrl(Page.EscapedTitle)} Read '{Page.Title}' on Wikipedia");
        }

        public void RenderInfobox(SimpleBuffer buffer, InfoboxItem infobox)
        {
            var title = string.IsNullOrEmpty(infobox.CustomTitle)
                ? "Quick Facts" :
                    $"Quick Facts: {infobox.CustomTitle}";

            buffer.EnsureAtLineStart();
            buffer.AppendLine($"## {title}");

            var navSuggestions = infobox.NavSuggestions;
            if (navSuggestions.Count() > 0)
            {
                //render navigation items at top
                foreach (var nav in navSuggestions)
                {
                    ContentRenderer.RenderNavSuggestion(buffer, nav);
                }
                //add a blank link, since nav suggestion can be long
                buffer.AppendLine();
            }

            foreach (var geo in infobox.GeoItems)
            {
                ContentRenderer.RenderGeo(buffer, geo);
            }

            foreach (var media in infobox.MediaItems)
            {
                ContentRenderer.RenderMedia(buffer, media as MediaItem);
            }

            buffer.EnsureAtLineStart();
            foreach (var item in infobox.ContentItems)
            {
                buffer.Append(item.Content);
            }
        }

        public string RenderSection(Section section)
        {
            sectionID++;

            SimpleBuffer buffer = new SimpleBuffer();
            if (section.HasNavSuggestions)
            {
                //render navigation items at top
                foreach (var nav in section.NavSuggestions)
                {
                    ContentRenderer.RenderNavSuggestion(buffer, nav);
                }
                //add a blank link, since nav suggestion can be long
                buffer.AppendLine();
            }

            //other content below, in order
            foreach (SectionItem item in section.GeneralContent)
            {
                if (item is MediaItem)
                {
                    ContentRenderer.RenderMedia(buffer, item as MediaItem);
                }
                else if (item is ContentItem)
                {
                    buffer.Append(((ContentItem)item).Content);
                }
            }
            foreach (var infoBox in section.Infoboxes)
            {
                RenderInfobox (buffer, infoBox);
            }

            if (section.Links.HasLinks && !ShouldExcludeSectionIndex(section))
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine($"=> {CommonUtils.ReferencesUrl(Page.Title, sectionID)} Section links: ({section.Links.Count} Articles)");
            }

            foreach (var subSection in section.SubSections)
            {
                buffer.Append(RenderSection(subSection));
            }

            //if a section has no content, don't write anything
            if (!buffer.HasContent)
            {
                return "";
            }

            if (!section.IsSpecial)
            {
                if (section.SectionDepth == 2)
                {
                    buffer.PrependLine($"## {section.Title}");
                }
                else
                {
                    //all other sections are at a level 3
                    buffer.PrependLine($"### {section.Title}");
                }
            }
            return buffer.Content;
        }

        private static bool ShouldExcludeSectionIndex(Section section)
            => CommonUtils.Settings.ArticleLinkSections.Contains(section.Title?.ToLower());
    }
}
