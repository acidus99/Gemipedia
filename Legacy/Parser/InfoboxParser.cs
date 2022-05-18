using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Legacy.Models;

namespace Gemipedia.Legacy.Parser
{
    public class InfoboxParser
    {

        //common block elements used inside a infobox 
        string[] blockElements = new string[] { "blockquote", "canvas", "div", "li", "ol", "p", "pre", "table", "ul" };

        List<MediaItem> mediaItems = new List<MediaItem>();

        ArticleLinkCollection ArticleLinks;

        StringBuilder buffer = new StringBuilder();

        public InfoboxParser()
        {
            ArticleLinks = new ArticleLinkCollection();
        }

        public InfoboxItem Parse(HtmlElement table)
        {

            var tableBodyRows = table.QuerySelector("tbody")?.Children ?? null;

            var title = ExtractTitle(table, tableBodyRows);

            //skip the TBODY
            
            Parse(tableBodyRows);
            
            return new InfoboxItem
                {
                    CustomTitle = title,
                    ArticleLinks = ArticleLinks,
                    Content = buffer.ToString(),
                    MediaItems = mediaItems
                };
        }

        private string ExtractTitle(HtmlElement table, IHtmlCollection<IElement> rows)
        {
            //first check for a caption
            var caption = table.QuerySelector("caption")?.TextContent.Trim() ?? null;
            if(!String.IsNullOrEmpty(caption))
            {
                return caption;
            }

            if (rows?.Length >= 1 && rows[0].ChildElementCount > 0 && rows[0].Children[0].NodeName == "TH")
            {
                return rows[0].Children[0].TextContent.Trim();
            }
            return null;
        }

        private void Parse(IHtmlCollection<IElement> rows)
        {
            if (rows == null)
            {
                return;
            }
            for (int i = 0; i < rows.Length; i++)
            {
                ParseRow(rows[i], i);
            }
        }

        private void ParseRow(IElement row, int index)
        {
            if(row.NodeName != "TR")
            {
                throw new ApplicationException("Non row in info box");
            }

            if(row.ChildElementCount == 0)
            {
                return;
            }

            if (IsHeader(row, index))
            {
                AddHeader(row);
            }
            else if (IsLabelAndData(row) || IsPseudoLabelAndData(row))
            {
                AddNameValue(row.Children[0], row.Children[1]);
            }
            else if (IsMedia(row))
            {
                AddMedia(row);
            }
        }

        private bool IsHeader(IElement row, int index)
            //don't consider the first row a header. We probably that for the title
            => index != 0 && (row.ChildElementCount == 1) && row.Children[0].NodeName == "TH";

        //it is a name/value pair using TH and TD cells?
        private bool IsLabelAndData(IElement row)
            => (row.ChildElementCount == 2) &&
                row.Children[0].NodeName == "TH" &&
                row.Children[1].NodeName == "TD";

        private bool IsMedia(IElement row)
            => (row.ChildElementCount == 1) &&
                (row.Children[0].ChildElementCount >= 1) &&
                (row.Children[0].Children?[0].QuerySelector("img") != null);

        //is it a name/value pair using TD and TD cells
        // check that the "name" cell doesn't contain complex block elements
        private bool IsPseudoLabelAndData(IElement row)
               => (row.ChildElementCount == 2) &&
                   row.Children[0].NodeName == "TD" &&
                   row.Children[1].NodeName == "TD" &&
                   !ContainsChildBlockElements(row.Children[0]);

        //Looks for the common block elements used in Infoboxes, so we know if th
        private bool ContainsChildBlockElements(IElement element)
        {
            foreach (var child in element.Children)
            {
                if(blockElements.Contains(child.NodeName.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddHeader(IElement row)
        {
            var textExtractor = new TextExtractor
            {
                ShouldCollapseNewlines = true
            };
            var text = textExtractor.GetText(row);
            buffer.AppendLine($"### {text}");
            ArticleLinks.MergeCollection(textExtractor.ArticleLinks);
        }

        private void AddNameValue(IElement nameCell, IElement valueCell)
        {
            var textExtractor = new TextExtractor
            {
                ShouldCollapseNewlines = true
            };
            var label = CleanLabel(textExtractor.GetText(nameCell));

            ArticleLinks.MergeCollection(textExtractor.ArticleLinks);
            HtmlTranslater htmlTranslater = new HtmlTranslater();
            var gemText = htmlTranslater.RenderGemtext(valueCell).Trim();
            ArticleLinks.MergeCollection(htmlTranslater.ArticleLinks);

            if (!gemText.Contains('\n'))
            {
                buffer.AppendLine($"{label}: {gemText}");
            }
            else
            {
                buffer.AppendLine($"{label}:");
                if (ShouldConvertToList(valueCell, gemText))
                {
                    //convert to a list
                    gemText.Trim().Split("\n").ToList()
                        .ForEach(x => buffer.AppendLine($"* {x}"));
                }
                else
                {
                    buffer.AppendLine(gemText);
                }
            }
        }

        private void AddMedia(IElement row)
        {
            var imageBlockParser = new ImageBlockParser();

            var imgContainer = row.Children[0].Children[0];
            var captionContainer = (row.Children[0].ChildElementCount >= 2) ? row.Children[0].Children[1] : null;

            var media = imageBlockParser.Convert(imgContainer, captionContainer);
            if(media != null)
            {
                mediaItems.Add(media);
            }
        }

        private string CleanLabel(string text)
        {
            //some labels attempt to look like a bulleted list, even though
            //each entry is a different row, and use a "•" character. remove it
            if (text.StartsWith("•") && text.Length > 1)
            {
                text = text.Substring(1);
            }
            //some label, especially those that use a TD for the name cell instead of a TH,
            //will include a ":" already in the text, if so remove it
            if (text.Length >= 2 && text.EndsWith(':'))
            {
                text = text.Substring(0, text.Length - 1);
            }
            return text.Trim();
        }

        //some info box data cells have multiple lines of text, separated by
        //a <BR> tags. These look better as lists in gemtext. Convert this if:
        // - not already a list or a link
        // - original HTML contains hard breaks
        private bool ShouldConvertToList(IElement cell, string gemText)
            => !gemText.StartsWith("* ") && !gemText.StartsWith("=> ") && cell.InnerHtml.Contains("<br>");

    }
}
