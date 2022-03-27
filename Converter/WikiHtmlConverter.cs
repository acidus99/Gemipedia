using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Converter.Models;
using Gemipedia.Converter.Parser;
using Gemipedia.Converter.Renderer;

namespace Gemipedia.Converter
{
    public class WikiHtmlConverter
    {
        ConverterSettings Settings;

        public WikiHtmlConverter(ConverterSettings settings)
        {
            Settings = settings;
            CommonUtils.Settings = settings;
        }

        public void Convert(string title, string wikiHtml, TextWriter writer)
        {
            //step 1: scope Html just to article content
            var contentRoot = GetContentRoot(wikiHtml);

            //step 2: remove known bad/unneeded tags
            RemoveTags(contentRoot);

            //step 3: parse content into model(s)
            var parsedPage = ParseContent(title, contentRoot);

            //step 4: render that model as gemtext to a specific TextWriter
            RenderArticle(parsedPage, writer);
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

        private ParsedPage ParseContent(string title, IElement contentRoot)
        {
            var parser = new WikiHtmlParser(Settings);
            return parser.ParseContent(title, contentRoot);
        }

        private void RenderArticle(ParsedPage parsedPage, TextWriter writer)
        {
            var renderer = new ArticleRenderer(Settings);
            renderer.RenderArticle(parsedPage, writer);
        }

    }
      
}