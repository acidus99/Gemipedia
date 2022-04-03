using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;
using System.Text;
using Gemipedia.Converter.Parser.Tables;

using Gemipedia.Converter.Models;

namespace Gemipedia.Converter.Parser
{
    public static class TableConverter
    {

        public static ContentItem Convert(HtmlElement element)
        {
            TableParser tableParser = new TableParser();
            var table = tableParser.ParseTable(element);

            var contents = TableRenderer.RenderTable(table);

            return new ContentItem
            {
                Content = contents,
                LinkedArticles = tableParser.LinkedArticles
            };
        }
    }
}