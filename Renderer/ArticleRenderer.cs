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

        SectionRenderer sectionRenderer;

        public ArticleRenderer(ConverterSettings settings)
        {
            Settings = settings;
        }

        public void RenderArticle(ParsedPage parsedPage, TextWriter writer)
        {
            Writer = writer;
            Page = parsedPage;
            sectionRenderer = new SectionRenderer(Settings, parsedPage.Title);

            RenderArticleHeader();
            foreach(var section in parsedPage.Sections)
            {
                Writer.Write(sectionRenderer.RenderSection(section));
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


    }
}
