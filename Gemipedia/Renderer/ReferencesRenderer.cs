using System;
using System.IO;
using System.Linq;
using System.Net;
using Gemipedia.Models;

namespace Gemipedia.Renderer;

public class ReferencesRenderer
{
    TextWriter Writer;
    ParsedPage Page;

    public void RenderReferences(ParsedPage parsedPage, TextWriter writer, int section)
    {
        Writer = writer;
        Page = parsedPage;

        if (section > 0)
        {
            RenderSectionReferences(section);
        }
        else
        {
            RenderAllReferences();
        }
    }

    private void RenderSectionReferences(int sectionNum)
    {

        var section = Page.GetSection(sectionNum);
        if (section != null)
        {
            var title = SectionName(section);

            Writer.WriteLine($"# References for {Page.Title}: {title}");
            Writer.WriteLine($"=> {RouteOptions.ArticleUrl(Page.Title)} Back to article");
            Writer.WriteLine($"=> {RouteOptions.ReferencesUrl(Page.Title)} See all references for article");
            Writer.WriteLine();
            Writer.WriteLine($"References to other articles in the '{title}' section");
            foreach (var linkTitle in section.Links.GetLinks())
            {
                Writer.WriteLine($"=> {RouteOptions.ArticleUrl(linkTitle)} {linkTitle}");
            }
        }
        Writer.WriteLine();
        Writer.WriteLine($"=> https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(Page.Title)} Source on Wikipedia");
    }

    private string SectionName(Section section)
        => section.IsSpecial ? "Summary Section" : section.Title;

    private void RenderAllReferences()
    {
        Writer.WriteLine($"# References for {Page.Title}");
        Writer.WriteLine($"=> {RouteOptions.ArticleUrl(Page.Title)} Back to article");
        Writer.WriteLine();
        Writer.WriteLine("References to other articles, organized by section");
        foreach (var subSection in Page.Sections.Where(x => !ShouldExcludeSectionIndex(x)))
        {
            RenderIndexForSection(subSection);
        }
        Writer.WriteLine();
        Writer.WriteLine($"=> https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(Page.Title)} Source on Wikipedia");
    }

    private void RenderIndexForSection(Section section)
    {
        //only display the section title if this section has links
        if (HasLinks(section))
        {
            if (!section.IsSpecial)
            {
                if (section.SectionDepth == 2)
                {
                    Writer.WriteLine($"## {section.Title}");
                }
                else
                {
                    //all other sections are at a level 3
                    Writer.WriteLine($"### {section.Title}");
                }
            }
            foreach (var linkTitle in section.Links.GetLinks())
            {
                Writer.WriteLine($"=> {RouteOptions.ArticleUrl(linkTitle)} {linkTitle}");
            }
        }
        if (section.HasSubSections)
        {
            foreach (var subSection in section.SubSections.Where(x => !ShouldExcludeSectionIndex(x)))
            {
                RenderIndexForSection(subSection);
            }
        }
    }

    //do we have any links which have no already been rendered?
    private bool HasLinks(Section section)
        => section.Links.HasLinks;

    private bool ShouldExcludeSectionIndex(Section section)
        => UserOptions.ArticleLinkSections.Contains(section.Title?.ToLower());
}