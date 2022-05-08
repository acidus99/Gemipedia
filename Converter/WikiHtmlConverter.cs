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

        public ParsedPage Parse(string title, string wikiHtml)
        {
            //step 1: scope Html just to article content
            var contentRoot = GetContentRoot(wikiHtml);

            //step 2: remove known bad/unneeded tags
            RemoveTags(contentRoot);

            //step 3: parse content into model(s)
            return ParseContent(title, contentRoot);
        }

        public void Convert(string title, string wikiHtml, TextWriter writer)
        {
            var parsedPage = Parse(title, wikiHtml);
            //step 4: render that model as gemtext to a specific TextWriter
            RenderArticle(parsedPage, writer);
        }

        public void ConvertImageGallery(string title, string wikiHtml, TextWriter writer)
        {
            var parsedPage = Parse(title, wikiHtml);
            RenderGallery(parsedPage, writer);
        }

        public void ConvertReferences(string title, string wikiHtml, TextWriter writer, int section = -1)
        {
            var parsedPage = Parse(title, wikiHtml);
            RenderReferences(parsedPage, writer, section);
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
            RemoveMatchingTags(contentRoot, "sup.reference");
            //all span holders for flag icons
            RemoveMatchingTags(contentRoot, "span.flagicon");
            //all <link> tags
            RemoveMatchingTags(contentRoot, "link");
            //all style tags
            RemoveMatchingTags(contentRoot, "style");
        }

        private void RemoveMatchingTags(IElement element, string selector)
            => element.QuerySelectorAll(selector).ToList().ForEach(x => x.Remove());

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

        private void RenderGallery(ParsedPage parsedPage, TextWriter writer)
        {
            GalleryRenderer renderer = new GalleryRenderer();
            renderer.RenderGallery(parsedPage, writer);
        }

        private void RenderReferences(ParsedPage parsedPage, TextWriter writer, int section)
        {
            var renderer = new ReferencesRenderer(Settings);
            renderer.RenderReferences(parsedPage, writer, section);
        }

    }
}