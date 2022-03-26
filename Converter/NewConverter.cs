using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

namespace Gemipedia.Converter
{
    public class NewConverter
    {
        ConverterSettings Settings;

        public NewConverter(ConverterSettings settings)
        {
            Settings = settings;
        }

        public void Convert(TextWriter writer, string title, string wikiHtml)
        {
            //step 1: scope Html just to article content
            var contentRoot = GetContentRoot(wikiHtml);

            //step 2: remove known bad/unneeded tags
            RemoveTags(contentRoot);

            //step 3: parse content into sections
            var sections = ParseSections(contentRoot);

            //step 4: render those sections as gemtext
            RenderArticle(writer, title, sections);
        }

        private IElement GetContentRoot(string wikiHtml)
        {
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();
            var document = parser.ParseDocument(wikiHtml);
            return document.QuerySelector("div.mw-parser-output");
        }


        //Removes tags we no we want need, and which make rendering harder
        private void RemoveTags(IElement contentRoot)
        {
            //all <sup> tags are used to link to references. 
            contentRoot.QuerySelectorAll("sup").ToList().ForEach(x => x.Remove());
            //all span holders for flag icons
            contentRoot.QuerySelectorAll("span.flagicon").ToList().ForEach(x => x.Remove());
        }

        private List<Section> ParseSections(IElement contentRoot)
        {
            Sectionizer sectionizer = new Sectionizer(Settings);
            return sectionizer.ExtractSections(contentRoot);
        }

        private void RenderArticle(TextWriter writer, string title, List<Section> sections)
        {
            GemTextRenderer renderer = new GemTextRenderer(Settings, writer);
            renderer.RenderArticle(title, sections);
        }

    }
      
}