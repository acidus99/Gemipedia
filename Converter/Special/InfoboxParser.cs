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
        List<MediaItem> mediaItems = new List<MediaItem>();
        Buffer buffer = new Buffer();
        TextExtractor textExtractor = new TextExtractor
        {
            ShouldCollapseNewlines = true
        };

        public InfoboxItem Parse(HtmlElement table)
        {
            var tableBodyRows = table.QuerySelector("tbody")?.Children ?? null;
            var title = ExtractTitle(table, tableBodyRows);
            Parse(tableBodyRows);
            
            return new InfoboxItem(buffer)
            {
                CustomTitle = title,
                MediaItems = mediaItems
            };
        }

        private void AddMedia(IElement row)
        {
            var imgContainer = row.Children[0].Children[0];
            var captionContainer = (row.Children[0].ChildElementCount >= 2) ? row.Children[0].Children[1] : null;

            var media = MediaParser.ConvertMedia(imgContainer, captionContainer);
            if (media != null)
            {
                mediaItems.Add(media);
            }
        }

        private void AddHeader(IElement row)
        {
            textExtractor.Extract(row);
            buffer.EnsureAtLineStart();
            buffer.AppendLine($"### {textExtractor.Content}");
            buffer.Links.Add(textExtractor);
        }

        private void AddWideValue(IElement valueCell)
        {
            //step 1, extract out the name
            var parser = new HtmlParser
            {
                ConvertListItems = false
            };
            parser.Parse(valueCell);
            //review HTML, append together any content items via a tmp
            //buffer, and add any media to our collection
            var htmlBuffer = new Buffer();
            foreach (SectionItem item in parser.GetItems())
            {
                if (item is MediaItem)
                {
                    var media = (MediaItem)item;
                    buffer.Links.Add(media.Links);
                    mediaItems.Add(media);
                }
                else if (item is ContentItem)
                {
                    var contentItem = (ContentItem)item;
                    htmlBuffer.Append(contentItem);
                }
            }

            buffer.Links.Add(htmlBuffer);
            buffer.EnsureAtLineStart();
            buffer.Append(htmlBuffer.Content);
        }

        private void AddNameValue(IElement nameCell, IElement valueCell)
        {
            //step 1, extract out the name
            textExtractor.Extract(nameCell);
            var label = CleanLabel(textExtractor.Content);
            buffer.Links.Add(textExtractor);


            //step 2: extract text but consider newlines
            var parser = new HtmlParser
            {
                ConvertListItems = false
            };
            parser.Parse(valueCell);

            //review HTML, append together any content items via a tmp
            //buffer, and add any media to our collection
            var htmlBuffer = new Buffer();
            foreach (SectionItem item in parser.GetItems())
            {
                if (item is MediaItem)
                {
                    var media = (MediaItem)item;
                    buffer.Links.Add(media.Links);
                    mediaItems.Add(media);
                }
                else if (item is ContentItem)
                {
                    var contentItem = (ContentItem)item;
                    htmlBuffer.Append(contentItem);
                }
            }

            buffer.Links.Add(htmlBuffer);
            var htmlContent = htmlBuffer.Content.Trim();

            buffer.EnsureAtLineStart();
            if (!htmlContent.Contains('\n'))
            {
                buffer.AppendLine($"{label}: {htmlContent}");
            }
            else
            {
                buffer.AppendLine($"{label}:");

                if (ShouldConvertToList(valueCell, htmlContent))
                {
                    //convert to a list
                    htmlContent.Trim().Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries).ToList()
                        .ForEach(x => buffer.AppendLine($"* {x}"));
                }
                else
                {
                    buffer.AppendLine(htmlContent);
                }
            }
            buffer.EnsureAtLineStart();
        }

        private string ExtractTitle(HtmlElement table, IHtmlCollection<IElement> rows)
        {
            //first check for a caption
            var caption = table.QuerySelector("caption")?.TextContent.Trim() ?? null;
            if(!String.IsNullOrEmpty(caption))
            {
                return caption;
            }

            if (rows?.Length >= 1 && rows[0].ChildElementCount ==1)
            {
                textExtractor.Extract(rows[0]);
                buffer.Links.Add(textExtractor);
                return textExtractor.Content.Trim();
            }
            return "";
        }

        private bool IsHeader(IElement row, int index)
            //don't consider the first row a header. We probably that for the title
            => index != 0 && (row.ChildElementCount == 1) && row.Children[0].NodeName == "TH";

        private bool IsMedia(IElement row)
            => (row.ChildElementCount == 1) &&
                (row.Children[0].ChildElementCount >= 1) &&
                (row.Children[0].Children?[0].QuerySelector("img") != null);

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
            else if (IsMedia(row))
            {
                AddMedia(row);
            }
            else if(row.Children.Length == 1)
            {
                AddWideValue(row.Children[0]);
            }
            else if (row.Children.Length == 2)
            {
                AddNameValue(row.Children[0], row.Children[1]);
            }

        }

        //some info box data cells have multiple lines of text, separated by
        //a <BR> tags. These look better as lists in gemtext. Convert this if:
        // - not already a list or a link
        // - original HTML contains hard breaks
        private bool ShouldConvertToList(IElement cell, string gemText)
            => !gemText.StartsWith("* ") && !gemText.StartsWith("=> ") && cell.InnerHtml.Contains("<br>");

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
    }
}
