using System;
using System.Linq;

using Gemipedia.Models;

namespace Gemipedia.Renderer
{
    public class SectionRenderer
    {
        SimpleBuffer buffer;
        ConverterSettings Settings;
        int sectionID = 0;
        string PageTitle;

        public SectionRenderer(ConverterSettings settings, string pageTitle)
        {
            Settings = settings;
            PageTitle = pageTitle;

            buffer = new SimpleBuffer();
        }

        public string RenderSection(Section section)
        {
            buffer.Reset();
            RenderSectionInternal(section);
            buffer.EnsureAtLineStart();
            return buffer.Content;
        }

        private void RenderSectionInternal(Section section)
        {
            sectionID++;

            if (section.HasNavSuggestions)
            {
                //render navigation items at top
                foreach (var nav in section.NavSuggestions)
                {
                    RenderNavSuggestion(nav);
                }
                //add a blank link, since nav suggestion can be long
                buffer.AppendLine();
            }

            //other content below, in order
            foreach (SectionItem item in section.GeneralContent)
            {
                if (item is MediaItem)
                {
                    RenderMedia(item as MediaItem);
                }
                else if (item is ContentItem)
                {
                    buffer.Append(((ContentItem)item).Content);
                }
            }
            foreach (var infoBox in section.Infoboxes)
            {
                RenderInfobox(infoBox);
            }

            if (section.Links.HasLinks && !ShouldExcludeSectionIndex(section))
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine($"=> {CommonUtils.ReferencesUrl(PageTitle, sectionID)} Section links: ({section.Links.Count} Articles)");
            }

            foreach (var subSection in section.SubSections)
            {
                buffer.Append(RenderSection(subSection));
            }

            //if a section has no content, don't write anything
            if (!buffer.HasContent)
            {
                return;
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
        }

        private void RenderInfobox(InfoboxItem infoBox)
        {
            buffer.EnsureAtLineStart();

            if (infoBox.HasContent)
            {
                var title = string.IsNullOrEmpty(infoBox.CustomTitle)
                    ? "Quick Facts" :
                        $"Quick Facts: {infoBox.CustomTitle}";

                buffer.AppendLine($"### {title}");
            }
            foreach (var media in infoBox.MediaItems)
            {
                RenderMedia(media);
            }
            if (infoBox.HasContent)
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine(infoBox.Content);
            }
        }

        private void RenderMedia(MediaItem media)
        {
            buffer.EnsureAtLineStart();

            if (media is VideoItem)
            {
                var video = (VideoItem)media;
                buffer.AppendLine($"=> {video.Url} Video Still: {video.Caption}");
                buffer.AppendLine($"=> {video.VideoUrl} Source Video: {video.VideoDescription}"); ;
            }
            else
            {
                buffer.AppendLine($"=> {media.Url} {media.Caption}");
            }
        }

        private void RenderNavSuggestion(NavSuggestionsItem nav)
        {
            var links = nav.Links.GetLinks();
            if (links.Count == 1)
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine($"=> {CommonUtils.ArticleUrl(links[0])} {nav.Content}");
            }
            else
            {
                buffer.EnsureAtLineStart();
                buffer.AppendLine($"({nav.Content})");
                foreach (var linkTitle in links)
                {
                    buffer.AppendLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
                }
            }
        }

        private bool ShouldExcludeSectionIndex(Section section)
            => Settings.ArticleLinkSections.Contains(section.Title?.ToLower());

    }
}
