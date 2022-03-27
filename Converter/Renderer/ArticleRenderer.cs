using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Renderer
{
    public class ArticleRenderer
    {
        ConverterSettings Settings;
        TextWriter Writer;
        LinkArticles linkedArticles;

        public ArticleRenderer(ConverterSettings settings)
        {
            Settings = settings;
            linkedArticles = new LinkArticles();
        }

        public void RenderArticle(ParsedPage parsedPage, TextWriter writer)
        {
            Writer = writer;

            RenderArticleTitle(parsedPage.Title);
            foreach(var section in parsedPage.Sections)
            {
                Writer.Write(RenderSection(section));
            }
            RenderIndex(parsedPage.Title);
        }

        private void RenderArticleTitle(string title)
        {
            Writer.WriteLine($"# {title}");
            Writer.WriteLine();
        }

        private void RenderIndex(string title)
        {
            Writer.WriteLine();
            Writer.WriteLine("## Index of References");
            foreach (var linkTitle in linkedArticles.GetLinks())
            {
                Writer.WriteLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
            }
            Writer.WriteLine();
            Writer.WriteLine($"=> https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(title)} View '{title}' on Wikipedia");
        }

        private string RenderSection(Section section)
        {
            StringBuilder sb = new StringBuilder();

            foreach(SectionItem item in section.Items)
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
