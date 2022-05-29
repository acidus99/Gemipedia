using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using Gemipedia.Models;
using Gemipedia.Converter;

namespace Gemipedia.Converter.Special
{
    public class InfoboxParser
    {
        InfoboxItem infobox = new InfoboxItem();
        Buffer buffer = new Buffer();
        bool shouldSkipFirst = false;

        TextExtractor textExtractor = new TextExtractor
        {
            ShouldCollapseNewlines = true
        };

        public InfoboxItem Parse(HtmlElement table)
        {
            var tableBodyRows = table.QuerySelector("tbody")?.Children ?? null;
            infobox.CustomTitle = ExtractTitle(table, tableBodyRows);
            ParseTableRows(tableBodyRows);

            return infobox;
        }

        //=====================================================================

        private void AddHeader(IElement row)
        {
            buffer.Reset();
            textExtractor.Extract(row);
            buffer.EnsureAtLineStart();
            buffer.AppendLine($"### {textExtractor.Content}");
            buffer.Links.Add(textExtractor);
            infobox.AddItem(new ContentItem(buffer));
        }

        private void AddMedia(IElement row)
        {
            //check for a montage
            var multi = row.QuerySelector("div.thumb.tmulti");
            if (multi != null)
            {
                infobox.AddItems(MediaParser.ConvertMultiple(multi));
                return;
            }

            var imgContainer = row.Children[0].Children[0];
            var captionContainer = (row.Children[0].ChildElementCount >= 2) ? row.Children[0].Children[1] : null;

            infobox.AddItem(MediaParser.ConvertMedia(imgContainer, captionContainer));
        }

        private void AddNameValue(IElement nameCell, IElement valueCell)
        {

            //step 1, extract out the name
            textExtractor.Extract(nameCell);
            var label = CleanLabel(textExtractor.Content);

            var parser = new HtmlParser
            {
                ConvertListItems = false,
            };
            parser.Parse(valueCell);

            var items = parser.GetItems();
            var contentItems = items.Where(x => x is ContentItem).Select(x => x as ContentItem).ToList();

            if(contentItems.Count == 0)
            {
                //just a placeholder label
                infobox.AddItem(new ContentItem
                {
                    Links = textExtractor.Links,
                    Content = label + ":" + "\n"
                });
                return;
            }

            if (parser.HasGeminiFormatting || contentItems.Count > 1)
            {
                //we need separate name/values
                infobox.AddItem(new ContentItem
                {
                    Links = textExtractor.Links,
                    Content = label + ":" + "\n"
                });

                infobox.AddItems(EnsureNewline(items));
                return;
            }
           
            //lets see if it has multiple lines or not
            var content = contentItems[0].Content.Trim();

            if (!content.Contains('\n'))
            {
                //name and value on same line
                infobox.AddItem(new ContentItem
                {
                    Links = textExtractor.Links,
                    Content = label + ": "
                });
                infobox.AddItems(EnsureNewline(items));
            }
            else
            {
                //we need separate name/values
                infobox.AddItem(new ContentItem
                {
                    Links = textExtractor.Links,
                    Content = label + ":" + "\n"
                });
                buffer.Reset();
                buffer.Links.Add(contentItems[0]);
                //convert to a list
                content.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    .ForEach(x => buffer.AppendLine($"* {x}"));
                infobox.AddItem(new ContentItem(buffer));
            }
        }

        //ensure that, when a sequence of items is rendered, the last item will make a new line
        private List<SectionItem> EnsureNewline(List<SectionItem> items)
        {
            var content = items.Where(x => x is ContentItem).Select(x => x as ContentItem);
            if(content.Count() > 0 && !content.Last().Content.EndsWith("\n"))
            {
                content.Last().Content += "\n";
            }
            return items;
        }

        private void AddWideValue(IElement valueCell)
        {
            //step 1, extract out the name
            var parser = new HtmlParser
            {
                ConvertListItems = false
            };
            parser.Parse(valueCell);
            infobox.AddItems(EnsureNewline(parser.GetItems()));
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

            text = text.Trim();
            //capitalize the first letter
            text = text.Substring(0, 1).ToUpper() + text.Substring(1);

            return text;
        }

        private string ExtractTitle(HtmlElement table, IHtmlCollection<IElement> rows)
        {
            //first check for a caption
            var caption = table.QuerySelector("caption")?.TextContent.Trim() ?? null;
            if (!String.IsNullOrEmpty(caption))
            {
                return caption;
            }

            if (rows?.Length >= 1 && rows[0].ChildElementCount == 1)
            {
                
                textExtractor.Extract(rows[0]);

                var title = textExtractor.Content.Trim();
                if (title.Length > 0)
                {
                    buffer.Links.Add(textExtractor);
                    shouldSkipFirst = true;
                    return title;
                }
            }
            return "";
        }

        private bool IsHeader(IElement row, int index)
            => (row.ChildElementCount == 1) && row.Children[0].NodeName == "TH";

        private bool IsMedia(IElement row)
            => (row.ChildElementCount == 1) &&
                (row.Children[0].ChildElementCount >= 1) &&
                (row.Children[0].Children?[0].QuerySelector("img") != null);

        private void ParseRow(IElement row, int index)
        {
            if(shouldSkipFirst && index == 0)
            {
                return;
            }
            if (row.NodeName != "TR")
            {
                throw new ApplicationException("Non row in info box");
            }

            if (row.ChildElementCount == 0)
            {
                return;
            }

            if (IsHeader(row, index))
            {
                AddHeader(row);
            }
            else if (IsMedia(row))
            {
                AddMedia(row);
            }
            else if (row.Children.Length == 1)
            {
                AddWideValue(row.Children[0]);
            }
            else if (row.Children.Length == 2)
            {
                AddNameValue(row.Children[0], row.Children[1]);
            }
        }

        private void ParseTableRows(IHtmlCollection<IElement> rows)
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
    }
}
