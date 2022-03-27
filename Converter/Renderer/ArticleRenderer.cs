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

        public ArticleRenderer(ConverterSettings settings)
        {
            Settings = settings;
        }

        public void RenderArticle(ParsedPage parsedPage, TextWriter writer)
        {
            Writer = writer;

            RenderArticleTitle(parsedPage.Title);
            foreach(var section in parsedPage.Sections)
            {
                Writer.Write(RenderSection(section));
            }
            RenderIndex(parsedPage);
        }

        private void RenderArticleTitle(string title)
        {
            Writer.WriteLine($"# {title}");
            Writer.WriteLine();
        }

        private void RenderIndex(ParsedPage parsedPage)
        {
            Writer.WriteLine();
            Writer.WriteLine("## Index of References");
            Writer.WriteLine("References to other articles, organized by section");
            foreach(Section section in parsedPage.Sections
                .Where(x=>x.LinkedArticles.Count > 0 && !Settings.ArticleLinkSections.Contains(x.Title?.ToLower())))
            {
                if(!section.IsSpecial)
                {
                    Writer.WriteLine($"### {section.Title}");
                }
                foreach (var linkTitle in section.LinkedArticles)
                {
                    Writer.WriteLine($"=> {CommonUtils.ArticleUrl(linkTitle)} {linkTitle}");
                }
            }
            Writer.WriteLine();
            Writer.WriteLine($"=> https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(parsedPage.Title)} View '{parsedPage.Title}' on Wikipedia");
        }

        private string RenderSection(Section section)
        {
            StringBuilder sb = new StringBuilder();

            foreach(SectionItem item in section.GetItems())
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
